import {execFileSync} from 'node:child_process';
import {mkdir, readFile, writeFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
import {gameSets, isConsumedSprite} from './selection.mjs';

const repo = fileURLToPath(new URL('../../', import.meta.url));
const args = process.argv.slice(2);
const sourceIndex = args.indexOf('--source');
if (sourceIndex < 0 || !args[sourceIndex + 1]) throw new Error('Pass --source PATH to a sprite repository checkout');
const source = path.resolve(args[sourceIndex + 1]);
const git = (...parameters) => execFileSync('git', ['-C', source, ...parameters], {encoding: 'utf8', maxBuffer: 32 * 1024 * 1024}).trim();
const manifestIndex = args.indexOf('--manifest');
const manifestPath = manifestIndex < 0 ? path.join(repo, 'tools/data/sprite-collection.json') : args[manifestIndex + 1];
const manifest = JSON.parse(await readFile(manifestPath, 'utf8'));
if (!/^[0-9a-f]{40}$/.test(manifest.revision)) throw new Error('Invalid pinned revision');
if (args.includes('--fetch-base')) git('fetch', '--no-tags', '--filter=blob:none', '--depth=1', 'origin', manifest.revision);
const revision = git('rev-parse', 'HEAD');
const sets = gameSets(await readFile(path.join(repo, 'Pkmds.Core/Utilities/PokeApiSpriteUrls.cs'), 'utf8'));
// Name/status comparisons with rename detection disabled need trees, not 12 GiB of image blobs.
const lines = git('diff', '--name-status', '--no-renames', manifest.revision, revision, '--', 'sprites/pokemon');
const changes = lines ? lines.split('\n').map(line => {
    const [status, file] = line.split('\t');
    return {status, file};
}).filter(item => isConsumedSprite(item.file, sets)) : [];
const report = {publishedRevision: manifest.revision, upstreamRevision: revision,
    added: changes.filter(item => item.status === 'A').map(item => item.file),
    removed: changes.filter(item => item.status === 'D').map(item => item.file),
    changed: changes.filter(item => item.status !== 'A' && item.status !== 'D').map(item => item.file)};
const summary = [
    '# Sprite repository changes', '',
    `Published: \`${manifest.revision}\``, `Upstream: \`${revision}\``, '',
    `Added: ${report.added.length}; changed: ${report.changed.length}; removed: ${report.removed.length}.`, '',
    ...changes.slice(0, 200).map(item => `- ${item.status}: \`${item.file}\``), '',
    'The JSON artifact contains the complete list. Regenerate, review and publish a new collection before changing the app URL.',
    'This check does not upload or delete assets.'
].join('\n') + '\n';
const outputIndex = args.indexOf('--output');
const output = outputIndex < 0 ? path.join(repo, 'artifacts/sprites') : args[outputIndex + 1];
await mkdir(output, {recursive: true});
await writeFile(path.join(output, 'upstream-changes.json'), JSON.stringify(report, null, 2) + '\n');
await writeFile(path.join(output, 'upstream-changes.md'), summary);
console.log(summary);
if (changes.length && args.includes('--fail-on-change')) process.exitCode = 1;
