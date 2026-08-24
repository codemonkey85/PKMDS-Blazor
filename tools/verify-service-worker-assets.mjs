import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { isAbsolute, resolve, relative } from 'node:path';

const publishRoot = resolve(process.argv[2] ?? 'release/wwwroot');
const manifestPath = resolve(publishRoot, 'service-worker-assets.js');
const manifestScript = await readFile(manifestPath, 'utf8');
const jsonStart = manifestScript.indexOf('{');
const jsonEnd = manifestScript.lastIndexOf('}');

if (jsonStart < 0 || jsonEnd <= jsonStart) {
    throw new Error(`Could not parse ${manifestPath}`);
}

const manifest = JSON.parse(manifestScript.slice(jsonStart, jsonEnd + 1));
const include = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.woff2$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.svg$/, /\.webp$/, /\.blat$/, /\.dat$/];
const exclude = [/^service-worker\.js$/, /^staticwebapp\.config\.json$/];
const assets = manifest.assets.filter(asset =>
    include.some(pattern => pattern.test(asset.url))
    && !exclude.some(pattern => pattern.test(asset.url)));

const failures = [];
await Promise.all(assets.map(async asset => {
    const assetPath = resolve(publishRoot, decodeURIComponent(asset.url));
    const relativePath = relative(publishRoot, assetPath);
    if (relativePath.startsWith('..') || isAbsolute(relativePath)) {
        failures.push(`${asset.url}: resolves outside the publish root`);
        return;
    }

    try {
        const bytes = await readFile(assetPath);
        const actualHash = `sha256-${createHash('sha256').update(bytes).digest('base64')}`;
        if (actualHash !== asset.hash) {
            failures.push(`${asset.url}: expected ${asset.hash}, got ${actualHash}`);
        }
    } catch (error) {
        failures.push(`${asset.url}: ${error.message}`);
    }
}));

if (failures.length > 0) {
    console.error(`Service-worker asset verification failed (${failures.length}/${assets.length}):`);
    for (const failure of failures) console.error(`- ${failure}`);
    process.exitCode = 1;
} else {
    console.log(`Verified ${assets.length} service-worker assets for manifest ${manifest.version}.`);
}
