import {createHash} from 'node:crypto';
import {readFile} from 'node:fs/promises';
import {pathToFileURL} from 'node:url';

export async function verifyPublishedCollection(bytes, fetchManifest = fetch) {
    const manifest = JSON.parse(bytes);
    const expectedBase = 'https://sprites.pkmds.app/collections/' + manifest.revision + '/';
    if (!/^[0-9a-f]{40}$/.test(manifest.revision) || manifest.publicBaseUrl !== expectedBase) throw new Error('Invalid collection URL');
    const response = await fetchManifest(expectedBase + 'manifest.json', {redirect: 'error', signal: AbortSignal.timeout(15000)});
    if (!response.ok || !response.headers.get('Content-Type')?.startsWith('application/json')) throw new Error('Published collection manifest unavailable: HTTP ' + response.status);
    const published = Buffer.from(await response.arrayBuffer());
    const hash = data => createHash('sha256').update(data).digest('hex');
    if (hash(published) !== hash(bytes)) throw new Error('Published collection manifest differs from the committed manifest');
    return {revision: manifest.revision, objectCount: manifest.objectCount, totalBytes: manifest.totalBytes, sha256: hash(bytes)};
}

if (process.argv[1] && pathToFileURL(process.argv[1]).href === import.meta.url) {
    const bytes = await readFile(new URL('../data/sprite-collection.json', import.meta.url));
    for (let attempt = 0; ; attempt++) {
        try {
            console.log('Published sprite collection verified: ' + JSON.stringify(await verifyPublishedCollection(bytes)));
            break;
        } catch (error) {
            if (attempt === 2) throw error;
            await new Promise(resolve => setTimeout(resolve, 1000 * (attempt + 1)));
        }
    }
}
