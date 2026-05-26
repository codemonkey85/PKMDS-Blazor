// Tiny static server for manual / Playwright MCP testing of the published
// release/wwwroot output. Same MIME handling and SPA fallback as run-bench.mjs.
// Listens on the port passed as argv[3] (default 8765) and logs the listen
// URL on startup. Stays running until SIGINT.

import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { join, resolve, relative, isAbsolute, extname, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..', '..');
const siteDir = resolve(process.argv[2] ?? join(repoRoot, 'release', 'wwwroot'));
const port = Number(process.argv[3] ?? 8765);

const MIME = {
    '.html': 'text/html; charset=utf-8',
    '.htm':  'text/html; charset=utf-8',
    '.js':   'application/javascript; charset=utf-8',
    '.mjs':  'application/javascript; charset=utf-8',
    '.css':  'text/css; charset=utf-8',
    '.json': 'application/json; charset=utf-8',
    '.wasm': 'application/wasm',
    '.dat':  'application/octet-stream',
    '.dll':  'application/octet-stream',
    '.pdb':  'application/octet-stream',
    '.png':  'image/png',
    '.jpg':  'image/jpeg',
    '.svg':  'image/svg+xml',
    '.webp': 'image/webp',
    '.ico':  'image/x-icon',
    '.woff': 'font/woff',
    '.woff2':'font/woff2',
};

const server = createServer(async (req, res) => {
    try {
        const url = new URL(req.url, 'http://localhost');
        let pathname = decodeURIComponent(url.pathname);
        if (pathname.endsWith('/')) pathname += 'index.html';

        const filePath = resolve(join(siteDir, pathname));
        const rel = relative(siteDir, filePath);
        if (rel.startsWith('..') || isAbsolute(rel)) {
            res.writeHead(400, { 'content-type': 'text/plain' });
            res.end('Bad request');
            return;
        }

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
        // Log full detail (incl. stack) to the console, but return a generic
        // message to the client so a stack trace isn't exposed in the HTTP
        // response (CodeQL js/stack-trace-exposure).
        console.error(err);
        res.writeHead(500, { 'content-type': 'text/plain' });
        res.end('Internal server error');
    }
});

server.listen(port, '127.0.0.1', () => {
    console.log(`Serving ${siteDir} on http://127.0.0.1:${port}`);
});
