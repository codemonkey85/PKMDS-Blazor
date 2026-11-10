import {createHash} from 'node:crypto';
import {mkdir, readFile, writeFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import path from 'node:path';

const repo = fileURLToPath(new URL('../../', import.meta.url));
const manifest = JSON.parse(await readFile(path.join(repo, 'tools/data/sprite-collection.json'), 'utf8'));
const origin = process.argv[2] || 'https://pkmds.app';
const selected = new Map();
for (const item of manifest.objects) {
    const relative = item.key.slice(manifest.prefix.length);
    // One Pikachu from each mapped game, plus transparent/shiny/female variants.
    if (relative.startsWith('pokemon/versions/') && /\/(?:transparent\/|shiny\/)?(?:female\/)?25\.(png|gif|webp)$/.test(relative)) selected.set(item.key, item);
}
for (const relative of ['other/home/0.png', 'other/home/994.png', 'other/home/25.png',
    'other/home/female/25.png', 'other/home/shiny/25.png', 'other/home/shiny/female/25.png',
    'other/home/201-b.png', 'other/home/10034.png', 'other/home/869-rainbow-swirl-ribbon-sweet.png']) {
    const key = manifest.prefix + 'pokemon/' + relative;
    const item = manifest.objects.find(image => image.key === key);
    if (!item) throw new Error('Required public probe missing from manifest: ' + relative);
    selected.set(key, item);
}
const results = [];
const images = [...selected.values()];
let next = 0;
await Promise.all(Array.from({length: 6}, async () => {
    while (next < images.length) {
        const item = images[next++];
        const response = await fetch('https://sprites.pkmds.app/' + item.key, {
            headers: {Origin: origin}, signal: AbortSignal.timeout(60000)
        });
        const bytes = Buffer.from(await response.arrayBuffer());
        const sha256 = createHash('sha256').update(bytes).digest('hex');
        const result = {key: item.key, status: response.status, bytes: bytes.length,
            sha256Matches: sha256 === item.sha256, contentType: response.headers.get('content-type'),
            cors: response.headers.get('access-control-allow-origin'), cacheControl: response.headers.get('cache-control')};
        result.passed = result.status === 200 && bytes.length === item.bytes && result.sha256Matches
            && result.contentType === item.contentType && (result.cors === '*' || result.cors === origin)
            && result.cacheControl === manifest.cacheControl;
        results.push(result);
    }
}));
const missing = await fetch(manifest.publicBaseUrl + 'pokemon/__intentionally_missing__.png', {
    headers: {Origin: origin}, signal: AbortSignal.timeout(30000)
});
const manifestResponse = await fetch(manifest.publicBaseUrl + 'manifest.json', {signal: AbortSignal.timeout(30000)});
const publishedManifest = await manifestResponse.json();
const report = {checkedAt: new Date().toISOString(), origin, images: results.length, results,
    missingStatus: missing.status, manifestMatches: JSON.stringify(publishedManifest) === JSON.stringify(manifest)};
report.passed = results.every(item => item.passed) && missing.status === 404 && report.manifestMatches;
await mkdir(path.join(repo, 'artifacts/sprites'), {recursive: true});
await writeFile(path.join(repo, 'artifacts/sprites/public-verification.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({passed: report.passed, images: results.length, missingStatus: report.missingStatus,
    manifestMatches: report.manifestMatches, failures: results.filter(item => !item.passed)}, null, 2));
if (!report.passed) process.exitCode = 1;
