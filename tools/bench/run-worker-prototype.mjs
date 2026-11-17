// Published-browser observation, not a dotnet test runner. Never uses a user profile.
import { chromium, webkit, firefox } from 'playwright';
import { createServer } from 'node:http';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { resolve, relative, isAbsolute, extname, dirname } from 'node:path';
import { parseArgs } from 'node:util';
import { platform, arch, release } from 'node:os';

const { values } = parseArgs({ options: {
    site: { type: 'string' }, runs: { type: 'string', default: '3' },
    browser: { type: 'string', default: 'chromium' },
    output: { type: 'string', default: 'measurements/legalization-worker.json' },
} });
if (!values.site) throw new Error('--site must name the isolated published prototype wwwroot');
const runs = Number(values.runs);
if (!Number.isInteger(runs) || runs < 1 || runs > 20) throw new Error('--runs must be 1–20');
const browserType = { chromium, webkit, firefox }[values.browser];
if (!browserType) throw new Error('Unsupported browser');
const site = resolve(values.site);
const mime = { '.html': 'text/html', '.js': 'text/javascript', '.json': 'application/json',
    '.wasm': 'application/wasm', '.dat': 'application/octet-stream' };
const server = createServer(async (request, response) => {
    try {
        const url = new URL(request.url, 'http://127.0.0.1');
        const file = resolve(site, '.' + decodeURIComponent(url.pathname === '/' ? '/index.html' : url.pathname));
        const rel = relative(site, file);
        if (rel.startsWith('..') || isAbsolute(rel)) { response.writeHead(400).end(); return; }
        let bytes;
        let encoding;
        if (request.headers['accept-encoding']?.includes('br')) {
            try { bytes = await readFile(file + '.br'); encoding = 'br'; } catch { /* identity fallback */ }
        }
        bytes ??= await readFile(file);
        response.writeHead(200, { 'content-type': mime[extname(file)] ?? 'application/octet-stream',
            'cache-control': 'no-store', 'content-length': bytes.length,
            ...(encoding ? { 'content-encoding': encoding } : {}) });
        response.end(bytes);
    } catch {
        response.writeHead(404).end('Not found');
    }
});
await new Promise((resolveListen, rejectListen) => {
    server.once('error', rejectListen);
    server.listen(0, '127.0.0.1', resolveListen);
});
let browser;
const report = { environment: { browser: values.browser,
    os: platform(), osRelease: release(), architecture: arch(), node: process.version,
    site, cache: 'fresh contexts; no-store HTTP; warm OS/server caches', compression: 'Brotli when available' },
    runs: [], failures: [] };
try {
    browser = await browserType.launch();
    report.environment.browserVersion = browser.version();
    for (let run = 0; run < runs; run++) {
        const context = await browser.newContext({ serviceWorkers: 'block' });
        try {
            const page = await context.newPage();
            const errors = [];
            const resources = [];
            context.on('response', response => {
                resources.push({ url: new URL(response.url()).pathname, status: response.status(),
                    encodedBytes: Number(response.headers()['content-length'] ?? 0),
                    encoding: response.headers()['content-encoding'] ?? 'identity' });
                if (response.status() >= 400) errors.push(`HTTP ${response.status()}: ${response.url()}`);
            });
            context.on('requestfailed', request => errors.push(`${request.url()}: ${request.failure()?.errorText}`));
            page.on('pageerror', error => errors.push(error.message));
            await page.goto(`http://127.0.0.1:${server.address().port}/?worker-prototype=1&worker-first=${run % 2}`);
            await page.waitForFunction(() => window.workerPrototypeResult !== undefined, undefined, { timeout: 120000 });
            const result = await page.evaluate(() => window.workerPrototypeResult);
            // Expected termination aborts can cancel runtime requests if a worker is
            // still starting; no failed asset is silently treated as a successful run.
            const observation = { run: run + 1, ...result, resources, errors };
            if (!result.ok || errors.length) {
                report.failures.push(observation);
                process.exitCode = 1;
                console.error(`Run ${run + 1} failed: ${result.error ?? errors.join('; ')}`);
            } else {
                report.runs.push(observation);
                console.log(`Run ${run + 1}: worker startup ${result.workerStartupMs.toFixed(1)} ms; lifecycle and data checks passed`);
            }
        } catch (error) {
            report.failures.push({ run: run + 1, error: error.stack });
            process.exitCode = 1;
            console.error(error.message);
        } finally {
            await context.close();
        }
    }
} catch (error) {
    report.failures.push({ error: error.stack });
    process.exitCode = 1;
    console.error(error.message);
} finally {
    await browser?.close();
    await new Promise(resolveClose => server.close(resolveClose));
    await mkdir(dirname(resolve(values.output)), { recursive: true });
    // Exclusive creation preserves every prior successful or failed observation.
    await writeFile(values.output, JSON.stringify(report, null, 2) + '\n', { flag: 'wx' });
}
