// Drives the /bench page in headless Chromium against a published Blazor build
// and appends runtime numbers to measurements/<label>.md alongside the size
// numbers produced by measure-publish.ps1. See issue #883.

import { createServer } from 'node:http';
import { readFile, stat, appendFile, mkdir } from 'node:fs/promises';
import { cpus, totalmem, platform, release, arch } from 'node:os';
import { join, resolve, relative, isAbsolute, extname, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..', '..');

const args = parseArgs(process.argv.slice(2));
const label = args.label;
if (!label) {
    console.error('usage: node run-bench.mjs --label <name> [--site <path>] [--output <path>] [--timeout <ms>] [--runs <count>] [--compression identity|br]');
    process.exit(2);
}
const siteDir = resolve(args.site ?? join(repoRoot, 'release', 'wwwroot'));
const outputPath = resolve(args.output ?? join(repoRoot, 'measurements', `${label}.md`));
const benchTimeoutMs = Number(args.timeout ?? 300_000);
const runs = Number(args.runs ?? 3);
const compression = args.compression ?? 'identity';
if (!Number.isInteger(runs) || runs < 1 || runs > 100 || !Number.isFinite(benchTimeoutMs) || benchTimeoutMs <= 0 || !['identity', 'br'].includes(compression)) {
    throw new Error('Expected runs=1..100, a positive timeout, and compression=identity|br.');
}
if (!/^[a-zA-Z0-9][a-zA-Z0-9._-]*$/.test(label)) throw new Error('Label must be a filename-safe identifier.');

await stat(siteDir).catch(() => {
    console.error(`Site directory not found: ${siteDir}`);
    process.exit(2);
});

const MIME = {
    '.html': 'text/html; charset=utf-8',
    '.htm': 'text/html; charset=utf-8',
    '.js': 'application/javascript; charset=utf-8',
    '.mjs': 'application/javascript; charset=utf-8',
    '.css': 'text/css; charset=utf-8',
    '.json': 'application/json; charset=utf-8',
    '.wasm': 'application/wasm',
    '.dat': 'application/octet-stream',
    '.dll': 'application/octet-stream',
    '.pdb': 'application/octet-stream',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.svg': 'image/svg+xml',
    '.webp': 'image/webp',
    '.ico': 'image/x-icon',
    '.woff': 'font/woff',
    '.woff2': 'font/woff2',
};

const server = createServer(async (req, res) => {
    try {
        const url = new URL(req.url, 'http://localhost');
        let pathname = decodeURIComponent(url.pathname);
        if (pathname.endsWith('/')) pathname += 'index.html';

        // Security: reject path traversal — resolved path must stay within siteDir.
        // Use path.relative for OS-agnostic comparison (Windows uses '\', not '/').
        const filePath = resolve(join(siteDir, pathname));
        const rel = relative(siteDir, filePath);
        if (rel.startsWith('..') || isAbsolute(rel)) {
            res.writeHead(400, { 'content-type': 'text/plain' });
            res.end('Bad request');
            return;
        }

        // SPA fallback only for extensionless routes (Blazor client-side navigation).
        // Asset requests with a known extension return 404 on miss so errors aren't masked.
        const servePath = extname(filePath) ? filePath : join(siteDir, 'index.html');

        let data;
        let encoding;
        try {
            // Use published Brotli siblings when requested; never compress during a run.
            const acceptsBrotli = (req.headers['accept-encoding'] ?? '').split(',').some(value => {
                const [name, ...parameters] = value.trim().split(';');
                const quality = parameters.find(parameter => parameter.trim().startsWith('q='));
                return name === 'br' && (!quality || Number(quality.trim().slice(2)) > 0);
            });
            if (compression === 'br' && acceptsBrotli) {
                data = await readFile(`${servePath}.br`).catch(error => {
                    if (error.code !== 'ENOENT') throw error;
                    return undefined;
                });
                if (data !== undefined) encoding = 'br';
            }
            data ??= await readFile(servePath);
        } catch {
            res.writeHead(404, { 'content-type': 'text/plain' });
            res.end('Not found');
            return;
        }

        const mime = MIME[extname(servePath).toLowerCase()] ?? 'application/octet-stream';
        const headers = { 'content-type': mime, 'cache-control': 'no-store', 'content-length': data.length, 'vary': 'Accept-Encoding' };
        if (encoding) headers['content-encoding'] = encoding;
        res.writeHead(200, headers);
        res.end(data);
    } catch (err) {
        // Log full detail (incl. stack) to the console, but return a generic
        // message to the client so a stack trace isn't exposed in the HTTP
        // response (CodeQL js/stack-trace-exposure).
        console.error(err);
        res.writeHead(500, { 'content-type': 'text/plain' });
        res.end('Internal server error');
    }
});

const port = await new Promise((resolvePort) => {
    server.listen(0, '127.0.0.1', () => resolvePort(server.address().port));
});
const baseUrl = `http://127.0.0.1:${port}`;
console.log(`Serving ${siteDir} on ${baseUrl}`);

let browser;
let failure;
const reports = [];
try {
    browser = await chromium.launch({ headless: true });
    for (let run = 1; run <= runs; run++) {
        // Startup and workloads use separate fresh contexts. The browser process
        // is reused; this is not a machine/engine-cache-cold startup measurement.
        const startup = await inFreshPage(async page => {
            await page.addInitScript(() => {
                const observer = new MutationObserver(() => {
                    if (!document.querySelector('.welcome-empty-state')) return;
                    window.pkmdsWelcomeDomReadyMs = performance.now();
                    observer.disconnect();
                });
                observer.observe(document, { childList: true, subtree: true });
            });
            await page.goto(baseUrl, { waitUntil: 'load', timeout: 60_000 });
            await page.locator('.welcome-empty-state').waitFor({ state: 'visible', timeout: benchTimeoutMs });
            return page.evaluate(() => ({
                WelcomeDomReadyMs: window.pkmdsWelcomeDomReadyMs,
                StartupResources: performance.getEntriesByType('resource').filter(entry => new URL(entry.name).origin === location.origin).map(entry => ({
                    Path: new URL(entry.name).pathname,
                    TransferBytes: entry.transferSize,
                    EncodedBodyBytes: entry.encodedBodySize,
                    DecodedBodyBytes: entry.decodedBodySize,
                })),
            }));
        });
        const report = await inFreshPage(async page => {
            const navStart = performance.now();
            await page.goto(`${baseUrl}/bench`, { waitUntil: 'load', timeout: 60_000 });
            await page.waitForSelector('#bench-root[data-bench-state="done"], #bench-root[data-bench-state="error"]', { timeout: benchTimeoutMs });
            const elapsedMs = performance.now() - navStart;
            if (await page.getAttribute('#bench-root', 'data-bench-state') === 'error') {
                throw new Error(`Bench page reported error:\n${await page.textContent('#bench-error')}`);
            }
            const result = JSON.parse(await page.textContent('#bench-result'));
            if (!result.Results?.length || result.Results.some(value =>
                ![value.TotalMs, value.MeanMs, value.MinMs, value.MaxMs, value.StdDevMs].every(Number.isFinite) ||
                !Number.isInteger(value.Iterations) || value.Iterations <= 0 ||
                !Number.isInteger(value.OpsPerIteration) || value.OpsPerIteration <= 0)) {
                throw new Error('Invalid benchmark results.');
            }
            result.NavToBenchmarkDoneMs = elapsedMs;
            result.UserAgent = await page.evaluate(() => navigator.userAgent);
            return result;
        });
        reports.push({ Run: run, ...startup, ...report });
        console.log(`Run ${run}/${runs}: welcome DOM ${startup.WelcomeDomReadyMs.toFixed(1)} ms; benchmark done ${report.NavToBenchmarkDoneMs.toFixed(1)} ms`);
    }
} catch (error) {
    failure = error;
} finally {
    try { await browser?.close(); }
    finally { server.close(); }
}

const measurement = {
    SchemaVersion: 1, Label: label, Site: siteDir, Date: new Date().toISOString(),
    BrowserVersion: browser?.version(), NodeVersion: process.version,
    Machine: { OS: platform(), Release: release(), Architecture: arch(), CPU: cpus()[0]?.model, LogicalProcessors: cpus().length, TotalMemoryBytes: totalmem() },
    CachePolicy: 'Fresh nonpersistent context per navigation; service workers blocked; no-store HTTP; browser process reused; engine/OS/server filesystem caches not reset.',
    Compression: compression, Runs: reports, Error: failure?.message,
};
await mkdir(dirname(outputPath), { recursive: true });
// JSON Lines retains prior invocations as well as every raw sample in this one.
const rawPath = outputPath.replace(/\.md$/i, '') + '.runtime.jsonl';
await appendFile(rawPath, JSON.stringify(measurement) + '\n');
if (failure) throw failure;
await appendFile(outputPath, reports.map(report => renderMarkdown(label, report)).join('') + `\nCache: ${measurement.CachePolicy}\n\nCompression: ${compression}. Raw results: ${rawPath}\n`);
console.log(`Appended ${runs} runtime runs to ${outputPath} and ${rawPath}`);

async function inFreshPage(action) {
    const context = await browser.newContext({ serviceWorkers: 'block', viewport: { width: 1280, height: 720 } });
    const errors = [];
    try {
        const page = await context.newPage();
        page.on('pageerror', error => errors.push(error.message));
        page.on('console', message => {
            if (message.type() === 'error') console.error('[page console]', message.text());
        });
        page.on('response', response => {
            if (response.url().startsWith(baseUrl + '/') && response.status() >= 400) errors.push(`HTTP ${response.status()} ${response.url()}`);
        });
        page.on('requestfailed', request => {
            if (request.url().startsWith(baseUrl + '/')) errors.push(`${request.url()}: ${request.failure()?.errorText}`);
        });
        const result = await action(page);
        if (errors.length) throw new Error(`Browser errors:\n${errors.join('\n')}`);
        return result;
    } finally {
        await context.close();
    }
}

function parseArgs(argv) {
    const out = {};
    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];
        const key = arg.slice(2);
        const next = argv[++i];
        if (!arg.startsWith('--') || !['label', 'site', 'output', 'timeout', 'runs', 'compression'].includes(key) ||
            Object.hasOwn(out, key) || !next || next.startsWith('--')) throw new Error(`Invalid argument: ${arg}`);
        out[key] = next;
    }
    return out;
}

function renderMarkdown(label, report) {
    const lines = [];
    lines.push('');
    lines.push(`## Runtime benchmark — run ${report.Run}`);
    lines.push('');
    lines.push(`- Label: ${label}`);
    lines.push(`- Date: ${report.Date}`);
    lines.push(`- Navigation → welcome DOM: **${report.WelcomeDomReadyMs.toFixed(1)} ms** (separate fresh-context home load; not time to interactive/paint)`);
    lines.push(`- Navigation → benchmark complete: **${report.NavToBenchmarkDoneMs.toFixed(1)} ms** (includes workloads; not startup)`);
    lines.push(`- UA: \`${report.UserAgent}\``);
    lines.push('');
    lines.push('| Workload | Iter | Ops/iter | Mean (ms) | Min (ms) | Max (ms) | StdDev (ms) | Ops/sec |');
    lines.push('| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |');
    for (const r of report.Results) {
        const opsPerSec = (r.Iterations * r.OpsPerIteration) / (r.TotalMs / 1000);
        lines.push(
            `| ${r.Name} | ${r.Iterations} | ${r.OpsPerIteration} | ${r.MeanMs.toFixed(3)} | ${r.MinMs.toFixed(3)} | ${r.MaxMs.toFixed(3)} | ${r.StdDevMs.toFixed(3)} | ${opsPerSec.toFixed(0)} |`
        );
    }
    lines.push('');
    return lines.join('\n');
}
