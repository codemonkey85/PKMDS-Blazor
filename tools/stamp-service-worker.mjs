import {readFile, writeFile, access} from 'node:fs/promises';
import {resolve} from 'node:path';
import {pathToFileURL} from 'node:url';
import {brotliCompressSync, constants, gzipSync} from 'node:zlib';

// Hosts can serve compressed siblings in preference to the raw worker. Stamp
// every existing representation so content negotiation cannot restore the token.
export async function stampServiceWorker(publishRoot, version) {
    if (!/^[A-Za-z0-9._-]+$/.test(version ?? '')) {
        throw new Error('Cache version must be a nonempty filename-safe identifier.');
    }

    const workerPath = resolve(publishRoot, 'service-worker.js');
    const source = await readFile(workerPath, 'utf8');
    const placeholder = '%%CACHE_VERSION%%';
    if (!source.includes(placeholder)) {
        throw new Error(`Cache-version placeholder not found in ${workerPath}.`);
    }

    const stamped = Buffer.from(source.replaceAll(placeholder, version));
    const outputs = [[workerPath, stamped]];
    for (const [suffix, compress] of [
        ['.br', bytes => brotliCompressSync(bytes, {params: {[constants.BROTLI_PARAM_QUALITY]: 11}})],
        ['.gz', gzipSync],
    ]) {
        const path = workerPath + suffix;
        try {
            await access(path);
        } catch (error) {
            if (error.code === 'ENOENT') continue;
            throw error;
        }
        outputs.push([path, compress(stamped)]);
    }

    for (const [path, bytes] of outputs) await writeFile(path, bytes);
    return outputs.map(([path]) => path);
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
    const [publishRoot, version, ...extra] = process.argv.slice(2);
    if (!publishRoot || !version || extra.length) {
        throw new Error('Usage: node tools/stamp-service-worker.mjs <publish-wwwroot> <cache-version>');
    }
    const paths = await stampServiceWorker(publishRoot, version);
    console.log(`Stamped cache version ${version} in ${paths.length} service-worker representations.`);
}
