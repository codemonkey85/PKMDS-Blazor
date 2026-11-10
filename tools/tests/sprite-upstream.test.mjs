import assert from 'node:assert/strict';
import {execFileSync, spawnSync} from 'node:child_process';
import {mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync} from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import test from 'node:test';
import {gameSets, isConsumedSprite} from '../sprites/selection.mjs';

const root = fileURLToPath(new URL('../../', import.meta.url));
const sets = gameSets(readFileSync(path.join(root, 'Pkmds.Core/Utilities/PokeApiSpriteUrls.cs'), 'utf8'));

test('monitor and exporter include front variants and exclude unused back sprites/formats', () => {
    for (const file of ['other/home/shiny/female/25.png', 'other/home/869-rainbow-swirl-ribbon-sweet.png',
        'versions/generation-ii/crystal/transparent/25.png', 'versions/generation-ix/scarlet-violet/shiny/female/25.webp']) {
        assert.equal(isConsumedSprite('sprites/pokemon/' + file, sets), true, file);
    }
    for (const file of ['other/official-artwork/25.png', 'versions/generation-ix/scarlet-violet/back/25.webp',
        'versions/generation-ix/scarlet-violet/25.png', 'versions/generation-v/black-white/animated/25.gif']) {
        assert.equal(isConsumedSprite('sprites/pokemon/' + file, sets), false, file);
    }
});

test('upstream report detects additions, patches and removals without downloading image blobs', () => {
    const temporary = mkdtempSync(path.join(os.tmpdir(), 'pkmds-sprites-'));
    try {
        const git = (...args) => execFileSync('git', ['-C', temporary, ...args], {encoding: 'utf8'}).trim();
        const put = (relative, contents) => {
            const file = path.join(temporary, relative);
            mkdirSync(path.dirname(file), {recursive: true});
            writeFileSync(file, contents);
        };
        git('init');
        git('config', 'user.email', 'sprite-tests@example.invalid');
        git('config', 'user.name', 'Sprite fixture');
        put('sprites/pokemon/other/home/25.png', 'original');
        put('sprites/pokemon/other/home/994.png', 'original');
        git('add', '.');
        git('commit', '-m', 'base');
        const revision = git('rev-parse', 'HEAD');
        const manifest = path.join(temporary, 'manifest.json');
        writeFileSync(manifest, JSON.stringify({revision}));
        rmSync(path.join(temporary, 'sprites/pokemon/other/home/25.png'));
        put('sprites/pokemon/other/home/994.png', 'patched');
        put('sprites/pokemon/other/home/10034.png', 'added');
        put('sprites/pokemon/versions/generation-ix/scarlet-violet/back/25.webp', 'ignored');
        git('add', 'sprites');
        git('commit', '-m', 'update');
        const output = path.join(temporary, 'report');
        const run = spawnSync(process.execPath, [path.join(root, 'tools/sprites/check-upstream.mjs'),
            '--source', temporary, '--manifest', manifest, '--output', output, '--fail-on-change'], {encoding: 'utf8'});
        assert.equal(run.status, 1, run.stderr);
        const report = JSON.parse(readFileSync(path.join(output, 'upstream-changes.json'), 'utf8'));
        assert.deepEqual(report.added, ['sprites/pokemon/other/home/10034.png']);
        assert.deepEqual(report.changed, ['sprites/pokemon/other/home/994.png']);
        assert.deepEqual(report.removed, ['sprites/pokemon/other/home/25.png']);
        assert.equal(report.publishedRevision, revision);
        assert.equal(report.upstreamRevision, git('rev-parse', 'HEAD'));
    } finally {
        rmSync(temporary, {recursive: true, force: true});
    }
});
