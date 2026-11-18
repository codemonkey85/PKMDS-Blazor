import assert from 'node:assert/strict';
import {mkdtemp, readFile, writeFile, rm, readdir} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join, resolve, sep} from 'node:path';
import test from 'node:test';
import {brotliCompressSync, brotliDecompressSync, gzipSync, gunzipSync} from 'node:zlib';
import {stampServiceWorker} from '../stamp-service-worker.mjs';

const source = "/* Manifest version: unchanged */\nconst CACHE_VERSION = '%%CACHE_VERSION%%';\n";

async function fixture(t, compressed = true) {
    const root = await mkdtemp(join(tmpdir(), 'pkmds-sw-stamp-'));
    t.after(async () => {
        assert.ok(resolve(root).startsWith(resolve(tmpdir()) + sep + 'pkmds-sw-stamp-'));
        await rm(root, {recursive: true, force: true});
    });
    await writeFile(join(root, 'service-worker.js'), source);
    await writeFile(join(root, 'service-worker-assets.js'), 'manifest must not change');
    if (compressed) {
        await writeFile(join(root, 'service-worker.js.br'), brotliCompressSync(Buffer.from(source)));
        await writeFile(join(root, 'service-worker.js.gz'), gzipSync(source));
    }
    return root;
}

test('all compressed representations decode to the stamped raw worker', async t => {
    const root = await fixture(t);
    assert.equal((await stampServiceWorker(root, '44375088891')).length, 3);
    const raw = await readFile(join(root, 'service-worker.js'));
    assert.equal(raw.toString(), source.replace('%%CACHE_VERSION%%', '44375088891'));
    assert.deepEqual(brotliDecompressSync(await readFile(join(root, 'service-worker.js.br'))), raw);
    assert.deepEqual(gunzipSync(await readFile(join(root, 'service-worker.js.gz'))), raw);
    assert.equal(await readFile(join(root, 'service-worker-assets.js'), 'utf8'), 'manifest must not change');
});

test('publishes without compressed siblings remain uncompressed', async t => {
    const root = await fixture(t, false);
    assert.equal((await stampServiceWorker(root, 'test-1')).length, 1);
    assert.deepEqual((await readdir(root)).sort(), ['service-worker-assets.js', 'service-worker.js']);
});

test('missing placeholder or invalid version fails before modifying any representation', async t => {
    const root = await fixture(t);
    const before = await readFile(join(root, 'service-worker.js.br'));
    await assert.rejects(stampServiceWorker(root, "bad'\nversion"), /Cache version/);
    assert.equal(await readFile(join(root, 'service-worker.js'), 'utf8'), source);
    await writeFile(join(root, 'service-worker.js'), 'no placeholder');
    await assert.rejects(stampServiceWorker(root, '123'), /placeholder not found/);
    assert.equal(await readFile(join(root, 'service-worker.js'), 'utf8'), 'no placeholder');
    assert.deepEqual(await readFile(join(root, 'service-worker.js.br')), before);
});
