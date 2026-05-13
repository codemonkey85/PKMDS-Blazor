// Drives the /bench page in headless Chromium against a published Blazor build
// and appends runtime numbers to measurements/<label>.md alongside the size
// numbers produced by measure-publish.ps1. See issue #883.

import { createServer } from 'node:http';
import { readFile, stat, appendFile } from 'node:fs/promises';
import { join, resolve, extname, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..', '..');

const args = parseArgs(process.argv.slice(2));
const label = args.label;
if (!label) {
    console.error('usage: node run-bench.mjs --label <name> [--site <path>] [--output <path>] [--timeout <ms>]');
    process.exit(2);
}
const siteDir = resolve(args.site ?? join(repoRoot, 'release', 'wwwroot'));
const outputPath = resolve(args.output ?? join(repoRoot, 'measurements', `${label}.md`));
const benchTimeoutMs = Number(args.timeout ?? 300_000);

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
    '.woff': 'font-woff',
    '.woff2': 'font/woff2',
};

const server = createServer(async (req, res) => {
    try {
        const url = new URL(req.url, 'http://localhost');
        let pathname = decodeURIComponent(url.pathname);
        if (pathname.endsWith('/')) pathname += 'index.html';

        // Security: reject path traversal — resolved path must stay within siteDir.
        const filePath = resolve(join(siteDir, pathname));
        if (!filePath.startsWith(siteDir + '/') && filePath !== siteDir) {
            res.writeHead(400, { 'content-type': 'text/plain' });
            res.end('Bad request');
            return;
        }

        // SPA fallback only for extensionless routes (Blazor client-side navigation).
        // Asset requests with a known extension return 404 on miss so errors aren't masked.
        const servePath = extname(filePath) ? filePath : join(siteDir, 'index.html');

        let data;
        try {
            data = await readFile(servePath);
        } catch {
            res.writeHead(404, { 'content-type': 'text/plain' });
            res.end('Not found');
            return;
        }

        const mime = MIME[extname(servePath).toLowerCase()] ?? 'application/octet-stream';
        res.writeHead(200, { 'content-type': mime, 'cache-control': 'no-store' });
        res.end(data);
    } catch (err) {
        res.writeHead(500, { 'content-type': 'text/plain' });
        res.end(String(err));
    }
});

const port = await new Promise((resolvePort) => {
    server.listen(0, '127.0.0.1', () => resolvePort(server.address().port));
});
const baseUrl = `http://127.0.0.1:${port}`;
console.log(`Serving ${siteDir} on ${baseUrl}`);

const browser = await chromium.launch({ headless: true });
let report;
try {
    const context = await browser.newContext();
    const page = await context.newPage();

    page.on('pageerror', (e) => console.error('[page error]', e.message));
    page.on('console', (msg) => {
        const text = msg.text();
        if (text.startsWith('BENCH_RESULT_JSON:')) return; // surfaced via the DOM scrape instead
        if (msg.type() === 'error') console.error('[page console]', text);
    });

    const navStart = Date.now();
    await page.goto(`${baseUrl}/bench`, { waitUntil: 'load', timeout: 60_000 });
    await page.waitForSelector('#bench-root[data-bench-state="done"], #bench-root[data-bench-state="error"]', {
        timeout: benchTimeoutMs,
    });
    const elapsedMs = Date.now() - navStart;

    const state = await page.getAttribute('#bench-root', 'data-bench-state');
    if (state === 'error') {
        const err = await page.textContent('#bench-error');
        throw new Error(`Bench page reported error:\n${err}`);
    }

    const resultText = await page.textContent('#bench-result');
    report = JSON.parse(resultText);
    report.NavToReadyMs = elapsedMs;
    report.UserAgent = await page.evaluate(() => navigator.userAgent);
} finally {
    await browser.close();
    server.close();
}

const md = renderMarkdown(label, report);
await appendFile(outputPath, md);
console.log(`Appended runtime numbers to ${outputPath}`);

function parseArgs(argv) {
    const out = {};
    for (let i = 0; i < argv.length; i++) {
        const arg = argv[i];
        if (arg.startsWith('--')) {
            const key = arg.slice(2);
            const next = argv[i + 1];
            if (next && !next.startsWith('--')) {
                out[key] = next;
                i++;
            } else {
                out[key] = true;
            }
        }
    }
    return out;
}

function renderMarkdown(label, report) {
    const lines = [];
    lines.push('');
    lines.push('## Runtime benchmark');
    lines.push('');
    lines.push(`- Label: ${label}`);
    lines.push(`- Date: ${report.Date}`);
    lines.push(`- Nav → ready: **${report.NavToReadyMs} ms**`);
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
