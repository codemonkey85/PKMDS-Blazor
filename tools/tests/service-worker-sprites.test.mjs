import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const source = readFileSync(new URL('../../Pkmds.Web/wwwroot/service-worker.published.js', import.meta.url), 'utf8');
const root = 'https://sprites.pkmds.app/collections/a7a3d62bb80a654ab5e01b488659929af0077c31/pokemon/';
const image = () => new Response('sprite', {headers: {'Content-Type': 'image/webp'}});

function worker({network = async () => image(), failOpen = false, failWrite = false, failDelete = false, stores = new Map()} = {}) {
    let requests = 0;
    let matches = 0;
    let scans = 0;
    const url = request => typeof request === 'string' ? request : request.url;
    const context = vm.createContext({
        URL, Request, Response, console,
        self: {origin: 'https://uat.example', assetsManifest: {version: 'test', assets: []},
            importScripts() {}, addEventListener() {}},
        fetch: async request => { requests++; return network(request); },
        caches: {open: async name => {
            if (failOpen) throw new Error('Cache Storage unavailable');
            if (!stores.has(name)) stores.set(name, new Map());
            const store = stores.get(name);
            return {
                match: async request => { matches++; return store.get(url(request))?.clone(); },
                put: async (request, response) => {
                    if (typeof failWrite === 'function' ? failWrite() : failWrite) throw new Error('Quota exceeded');
                    store.set(url(request), response.clone());
                },
                keys: async () => { scans++; return [...store.keys()].map(url => new Request(url)); },
                delete: async request => {
                    if (typeof failDelete === 'function' ? failDelete() : failDelete) throw new Error('Delete failed');
                    return store.delete(url(request));
                },
            };
        }},
    });
    vm.runInContext(source, context);
    return {
        get: (url = root + 'versions/generation-ix/scarlet-violet/25.webp') =>
            context.onFetch({request: new Request(url, {mode: 'cors'})}),
        stores,
        requests: () => requests,
        storageReads: () => ({matches, scans}),
    };
}

test('30 identical sprites share one network fetch and have independent response bodies', async () => {
    const sw = worker();
    const responses = await Promise.all(Array.from({length: 30}, () => sw.get()));
    assert.deepEqual(await Promise.all(responses.map(response => response.text())), Array(30).fill('sprite'));
    assert.equal(sw.requests(), 1);
    assert.equal(await (await sw.get()).text(), 'sprite');
    assert.equal(sw.requests(), 1);
});

test('distinct normal, shiny and female URLs remain distinct cache entries', async () => {
    const sw = worker();
    for (const suffix of ['25.png', 'shiny/25.png', 'female/25.png']) {
        await sw.get(root + 'other/home/' + suffix);
    }
    assert.equal(sw.requests(), 3);
    assert.equal(sw.stores.get('pkmds-sprites-v2').size, 3);
});

test('404s and HTML responses are not cached and may succeed on a later retry', async () => {
    let attempt = 0;
    const sw = worker({network: async () => {
        attempt++;
        if (attempt === 1) return new Response('', {status: 404});
        if (attempt === 2) return new Response('<html/>', {headers: {'Content-Type': 'text/html'}});
        return image();
    }});
    assert.equal((await sw.get()).status, 404);
    await sw.get();
    await sw.get();
    await sw.get();
    assert.equal(sw.requests(), 3);
});

test('hosted misses revalidate HTTP caches so previously missing images can recover', async () => {
    let attempt = 0;
    const sw = worker({network: async request => {
        assert.equal(request.cache, 'no-cache');
        assert.equal(request.mode, 'cors');
        attempt++;
        return attempt === 1
            ? new Response('', {status: 404, headers: {'Cache-Control': 'max-age=14400'}})
            : image();
    }});
    assert.equal((await sw.get()).status, 404);
    assert.equal((await sw.get()).status, 200);
    assert.equal((await sw.get()).status, 200);
    assert.equal(sw.requests(), 2);
});

test('cached sprites work offline; uncached failures resolve as image errors', async () => {
    let offline = false;
    const sw = worker({network: async () => {
        if (offline) throw new TypeError('Offline');
        return image();
    }});
    await sw.get();
    offline = true;
    assert.equal((await sw.get()).status, 200);
    assert.equal((await sw.get(root + 'missing.webp')).status, 504);
});

for (const failure of ['failOpen', 'failWrite']) {
    test(`${failure} does not discard a successful network image`, async () => {
        const sw = worker({[failure]: true});
        assert.equal(await (await sw.get()).text(), 'sprite');
    });
}

test('hosted cache is bounded and evicts oldest entries', async () => {
    const sw = worker();
    for (let i = 0; i < 515; i++) await sw.get(root + i + '.webp');
    const cache = sw.stores.get('pkmds-sprites-v2');
    assert.equal(cache.size, 512);
    assert.equal(cache.has(root + '0.webp'), false);
    assert.equal(cache.has(root + '514.webp'), true);
});

test('30 new sprites scan 512 existing entries only once', async () => {
    const sw = worker();
    const cache = new Map(Array.from({length: 512}, (_, index) => [root + 'old-' + index + '.webp', image()]));
    sw.stores.set('pkmds-sprites-v2', cache);

    const responses = await Promise.all(Array.from({length: 30}, (_, index) => sw.get(root + 'new-' + index + '.webp')));

    assert.equal(responses.every(response => response.ok), true);
    assert.deepEqual(sw.storageReads(), {scans: 1, matches: 512 + 30});
    assert.equal(cache.size, 512);
    assert.equal(cache.has(root + 'old-29.webp'), false);
    assert.equal(cache.has(root + 'old-30.webp'), true);
    assert.equal(cache.has(root + 'new-29.webp'), true);
});

test('a restarted worker rebuilds byte accounting from the existing cache', async () => {
    const network = async () => new Response('animation', {
        headers: {'Content-Type': 'image/webp', 'Content-Length': String(50 * 1024 * 1024)}
    });
    const first = worker({network});
    await first.get(root + '0.webp');
    await first.get(root + '1.webp');
    const restarted = worker({network, stores: first.stores});

    await restarted.get(root + '2.webp');

    const cache = restarted.stores.get('pkmds-sprites-v2');
    assert.equal(restarted.storageReads().scans, 1);
    assert.equal(cache.size, 2);
    assert.equal(cache.has(root + '0.webp'), false);
    assert.equal(cache.has(root + '1.webp'), true);
    assert.equal(cache.has(root + '2.webp'), true);
});

test('failed maintenance rebuilds accounting once and then resumes incremental writes', async () => {
    let fail = true;
    const sw = worker({failWrite: () => fail});
    assert.equal((await sw.get(root + 'failed.webp')).status, 200);
    fail = false;

    await sw.get(root + 'first.webp');
    await sw.get(root + 'second.webp');

    assert.equal(sw.storageReads().scans, 2);
    const cache = sw.stores.get('pkmds-sprites-v2');
    assert.equal(cache.size, 2);
    assert.equal(cache.has(root + 'failed.webp'), false);
});

test('a failed eviction returns the image and rebuilds the byte index on the next write', async () => {
    let fail = false;
    const sw = worker({failDelete: () => fail, network: async () => new Response('animation', {
        headers: {'Content-Type': 'image/webp', 'Content-Length': String(50 * 1024 * 1024)}
    })});
    await sw.get(root + '0.webp');
    await sw.get(root + '1.webp');
    fail = true;
    assert.equal((await sw.get(root + '2.webp')).status, 200);
    fail = false;

    assert.equal((await sw.get(root + '3.webp')).status, 200);

    assert.equal(sw.storageReads().scans, 2);
    const cache = sw.stores.get('pkmds-sprites-v2');
    assert.deepEqual([...cache.keys()], [root + '2.webp', root + '3.webp']);
});

test('only the exact collection prefix uses the hosted sprite cache', async () => {
    const sw = worker();
    for (const url of ['https://sprites.pkmds.app/other/image.png',
        'https://sprites.pkmds.app.evil.example/pokemon/25.png',
        'https://raw.githubusercontent.com/owner/repo/main/image.png',
        'https://sprites.pkmds.app/pokemon/25.png',
        'https://sprites.pkmds.app/collections/other-revision/pokemon/25.png']) {
        await sw.get(url);
        await sw.get(url);
    }
    assert.equal(sw.requests(), 10);
    assert.equal(sw.stores.has('pkmds-sprites-v2'), false);
});

test('large animations evict oldest sprites by byte budget before reaching the entry limit', async () => {
    const sw = worker({network: async () => new Response('animation', {
        headers: {'Content-Type': 'image/webp', 'Content-Length': String(50 * 1024 * 1024)}
    })});
    for (let index = 0; index < 4; index++) await sw.get(root + index + '.webp');
    const cache = sw.stores.get('pkmds-sprites-v2');
    assert.equal(cache.size, 2);
    assert.equal(cache.has(root + '0.webp'), false);
    assert.equal(cache.has(root + '3.webp'), true);
});

test('concurrent distinct animations finish inside the byte budget', async () => {
    const sw = worker({network: async () => new Response('animation', {
        headers: {'Content-Type': 'image/webp', 'Content-Length': String(50 * 1024 * 1024)}
    })});
    const responses = await Promise.all(Array.from({length: 10}, (_, index) => sw.get(root + index + '.webp')));
    assert.equal(responses.every(response => response.ok), true);
    assert.equal(sw.stores.get('pkmds-sprites-v2').size, 2);
});

test('bundled sprites are cached lazily in the app cache', async () => {
    const sw = worker();
    const url = 'https://uat.example/_content/Pkmds.Rcl/sprites/big/25.png';
    await sw.get(url);
    await sw.get(url);
    assert.equal(sw.requests(), 1);
    assert.equal(sw.stores.has('pkmds-sprites-v2'), false);
    assert.equal([...sw.stores.keys()].some(key => key.startsWith('offline-cache-')), true);
});
