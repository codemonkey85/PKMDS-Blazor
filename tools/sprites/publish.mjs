import {createHash} from 'node:crypto';
import {execFileSync, spawn} from 'node:child_process';
import {mkdir, readFile, writeFile} from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, '../..');
const args = process.argv.slice(2);
const command = args.shift();
function option(name, fallback) {
    const index = args.indexOf('--' + name);
    return index < 0 ? fallback : args[index + 1];
}
const manifestPath = path.resolve(option('manifest', path.join(repo, 'tools/data/sprite-collection.json')));
const source = path.resolve(option('source', path.join(repo, '../sprites')));
const reportPath = path.resolve(option('report', path.join(repo, 'artifacts/sprites/' + command + '.json')));
const concurrency = Number(option('concurrency', '8'));
if (!Number.isInteger(concurrency) || concurrency < 1 || concurrency > 32) throw new Error('Concurrency must be 1..32');
const hash = (bytes, algorithm = 'sha256') => createHash(algorithm).update(bytes).digest('hex');
const git = (...parameters) => execFileSync('git', ['-C', source, ...parameters], {encoding: 'utf8', maxBuffer: 32 * 1024 * 1024}).trim();
const cacheControl = 'public, max-age=31536000, immutable';
async function json(file, value) {
    await mkdir(path.dirname(file), {recursive: true});
    await writeFile(file, JSON.stringify(value, null, 2) + '\n');
}
async function parallel(items, action) {
    let next = 0;
    const errors = [];
    await Promise.all(Array.from({length: concurrency}, async () => {
        while (next < items.length && errors.length === 0) {
            const item = items[next++];
            try { await action(item); } catch (error) { errors.push(error); }
        }
    }));
    if (errors.length) throw errors[0];
}
async function rclone(parameters, capture = false) {
    const executable = option('rclone', 'rclone');
    return new Promise((resolve, reject) => {
        const child = spawn(executable, parameters, {stdio: capture ? ['ignore', 'pipe', 'inherit'] : 'inherit', windowsHide: true});
        const chunks = [];
        if (capture) child.stdout.on('data', chunk => chunks.push(chunk));
        child.on('error', reject);
        child.on('close', code => code === 0 ? resolve(Buffer.concat(chunks).toString('utf8'))
            : reject(new Error('rclone ' + parameters[0] + ' exited with code ' + code)));
    });
}
function checkSource(revision) {
    if (git('rev-parse', 'HEAD') !== revision) throw new Error('Source HEAD differs from the pinned revision');
    if (git('status', '--porcelain', '--untracked-files=all', '--', 'sprites/pokemon')) throw new Error('Source sprite tree has local changes');
}

if (command === 'manifest') {
    const revision = git('rev-parse', 'HEAD');
    checkSource(revision);
    const mapping = await readFile(path.join(repo, 'Pkmds.Core/Utilities/PokeApiSpriteUrls.cs'), 'utf8');
    const sets = [...mapping.matchAll(/new\("(generation-[^" ]+)", "(png|gif|webp)"\)/g)]
        .map(([, directory, extension]) => ({directory, extension}));
    if (sets.length < 20) throw new Error('Could not extract all game sprite sets');
    const tracked = git('ls-tree', '-r', '--name-only', revision, '--', 'sprites/pokemon').split('\n');
    const variants = new Set(['', 'female', 'shiny', 'shiny/female', 'transparent', 'transparent/female']);
    const selected = tracked.filter(file => {
        if (/^sprites\/pokemon\/other\/home\/(?:shiny\/)?(?:female\/)?[^/]+\.png$/.test(file)) return true;
        return sets.some(set => {
            const prefix = 'sprites/pokemon/versions/' + set.directory + '/';
            if (!file.startsWith(prefix) || !file.endsWith('.' + set.extension)) return false;
            const relative = file.slice(prefix.length);
            return variants.has(path.posix.dirname(relative) === '.' ? '' : path.posix.dirname(relative));
        });
    }).sort();
    const prefix = 'collections/' + revision + '/';
    const objects = [];
    await parallel(selected, async file => {
        const bytes = await readFile(path.join(source, file));
        const extension = path.extname(file).slice(1);
        objects.push({key: prefix + file.slice('sprites/'.length), bytes: bytes.length,
            sha256: hash(bytes), md5: hash(bytes, 'md5'), contentType: 'image/' + extension});
    });
    objects.sort((a, b) => a.key < b.key ? -1 : a.key > b.key ? 1 : 0);
    const groups = {};
    for (const item of objects) {
        const relative = item.key.slice(prefix.length);
        const group = relative.startsWith('pokemon/other/') ? 'home' : relative.split('/').slice(2, 4).join('/');
        groups[group] ??= {objects: 0, bytes: 0};
        groups[group].objects++;
        groups[group].bytes += item.bytes;
    }
    const manifest = {schemaVersion: 1, source: 'https://github.com/codemonkey85/sprites', revision, prefix,
        publicBaseUrl: 'https://sprites.pkmds.app/' + prefix, cacheControl,
        objectCount: objects.length, totalBytes: objects.reduce((sum, item) => sum + item.bytes, 0), groups, objects};
    await json(manifestPath, manifest);
    console.log(JSON.stringify({manifestPath, objectCount: manifest.objectCount, totalBytes: manifest.totalBytes, groups}));
} else if (command === 'status') {
    const manifest = JSON.parse(await readFile(manifestPath, 'utf8'));
    const remote = option('remote', 'pkmdssprites:pkmds-sprites-test');
    console.log(await rclone(['size', remote.replace(/\/$/, '') + '/' + manifest.prefix + 'pokemon/', '--json'], true));
} else if (command === 'upload' || command === 'verify') {
    const manifestBytes = await readFile(manifestPath);
    const manifest = JSON.parse(manifestBytes);
    if (!/^[0-9a-f]{40}$/.test(manifest.revision) || manifest.prefix !== 'collections/' + manifest.revision + '/') throw new Error('Invalid collection prefix');
    if (manifest.objectCount !== manifest.objects.length || new Set(manifest.objects.map(x => x.key)).size !== manifest.objectCount) throw new Error('Invalid manifest count/duplicate keys');
    for (const item of manifest.objects) {
        if (!item.key.startsWith(manifest.prefix + 'pokemon/') || item.key.includes('..')
            || !/^[0-9a-f]{64}$/.test(item.sha256) || !/^[0-9a-f]{32}$/.test(item.md5)) throw new Error('Invalid manifest object');
    }
    const remote = option('remote', 'pkmdssprites:pkmds-sprites-test');
    const destination = remote.replace(/\/$/, '') + '/' + manifest.prefix;
    const outputDirectory = path.join(repo, 'artifacts/sprites');
    await mkdir(outputDirectory, {recursive: true});
    const filesPath = path.join(outputDirectory, 'files.txt');
    const sumsPath = path.join(outputDirectory, 'MD5SUMS');
    const relative = item => item.key.slice(manifest.prefix.length);
    await writeFile(filesPath, manifest.objects.map(relative).join('\n') + '\n');
    await writeFile(sumsPath, manifest.objects.map(item => item.md5 + '  ' + relative(item)).join('\n') + '\n');
    if (command === 'upload') {
        checkSource(manifest.revision);
        // Bind the copy to the committed manifest, not just to filenames in the current checkout.
        await parallel(manifest.objects, async item => {
            const bytes = await readFile(path.join(source, 'sprites', relative(item)));
            if (bytes.length !== item.bytes || hash(bytes) !== item.sha256 || hash(bytes, 'md5') !== item.md5) throw new Error('Source checksum differs: ' + item.key);
        });
        await rclone(['copy', path.join(source, 'sprites'), destination, '--files-from-raw', filesPath,
            '--checksum', '--immutable', '--fast-list', '--transfers', String(concurrency), '--checkers', '32',
            '--s3-upload-cutoff', '256Mi', '--metadata', '--metadata-set', 'cache-control=' + cacheControl,
            '--metadata-set', 'source-revision=' + manifest.revision, '--stats', '30s', '--stats-one-line', '--stats-log-level', 'NOTICE',
            '--retries', '5', '--low-level-retries', '10']);
    }
    // Check against the manifest's hashes, independently of the local files used for the copy.
    await rclone(['checksum', 'MD5', sumsPath, destination, '--one-way', '--checkers', '32',
        '--combined', path.join(outputDirectory, 'checksum-results.txt')]);
    const listing = JSON.parse(await rclone(['lsjson', destination + 'pokemon/', '--recursive', '--files-only', '--hash', '--no-modtime', '--no-mimetype'], true));
    const expected = new Map(manifest.objects.map(item => [relative(item).slice('pokemon/'.length), item]));
    if (listing.length !== expected.size) throw new Error('Remote object count differs from the manifest');
    for (const actual of listing) {
        const item = expected.get(actual.Path);
        if (!item || actual.Size !== item.bytes || actual.Hashes?.MD5 !== item.md5) throw new Error('Remote object differs: ' + actual.Path);
    }
    if (command === 'upload') {
        // Publish the manifest only after every image has passed completeness/checksum verification.
        await rclone(['copyto', manifestPath, destination + 'manifest.json', '--checksum', '--immutable',
            '--metadata', '--metadata-set', 'cache-control=' + cacheControl]);
    }
    const report = {checkedAt: new Date().toISOString(), revision: manifest.revision, manifestSha256: hash(manifestBytes),
        objectCount: manifest.objectCount, totalBytes: manifest.totalBytes, method: 'rclone S3', passed: true};
    await json(reportPath, report);
    console.log(JSON.stringify(report));
} else {
    throw new Error('Usage: node tools/sprites/publish.mjs manifest|upload|verify|status [--source PATH] [--manifest PATH] [--report PATH] [--rclone PATH] [--remote NAME:BUCKET] [--concurrency 1..32]');
}
