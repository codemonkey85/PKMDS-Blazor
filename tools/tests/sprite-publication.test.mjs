import assert from 'node:assert/strict';
import test from 'node:test';
import {verifyPublishedCollection} from '../sprites/verify-published.mjs';

const revision = 'a'.repeat(40);
const bytes = Buffer.from(JSON.stringify({revision, publicBaseUrl: 'https://sprites.pkmds.app/collections/' + revision + '/', objectCount: 2, totalBytes: 100}));

test('publication gate accepts the exact published manifest at its immutable URL', async () => {
    const result = await verifyPublishedCollection(bytes, async (url, options) => {
        assert.equal(url, 'https://sprites.pkmds.app/collections/' + revision + '/manifest.json');
        assert.equal(options.redirect, 'error');
        return new Response(bytes, {headers: {'Content-Type': 'application/json'}});
    });
    assert.equal(result.objectCount, 2);
    assert.equal(result.totalBytes, 100);
});

test('publication gate blocks missing, mismatched and HTML manifests', async () => {
    for (const response of [new Response('', {status: 404}),
        new Response('{}', {headers: {'Content-Type': 'application/json'}}),
        new Response('<html/>', {headers: {'Content-Type': 'text/html'}})]) {
        await assert.rejects(verifyPublishedCollection(bytes, async () => response));
    }
});
