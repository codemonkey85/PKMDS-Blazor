import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const source = readFileSync(new URL('../../Pkmds.Web/wwwroot/service-worker.published.js', import.meta.url), 'utf8');
const root = 'https://sprites.pkmds.app/pokemon/';
const image = () => new Response('sprite', {headers: {'Content-Type': 'image/webp'}});

function worker({network = async () => image(), failOpen = false, failWrite = false} = {}) {
    const stores = new Map();
    let requests = 0;
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
                match: async request => store.get(request.url)?.clone(),
                put: async (request, response) => {
                    if (failWrite) throw new Error('Quota exceeded');
                    store.set(request.url, response.clone());
                },
                keys: async () => [...store.keys()].map(url => new Request(url)),
                delete: async request => store.delete(request.url),
            };
        }},
    });
    vm.runInContext(source, context);
    return {
        get: (url = root + 'versions/generation-ix/scarlet-violet/25.webp') =>
            context.onFetch({request: new Request(url, {mode: 'cors'})}),
        stores,
        requests: () => requests,
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

test('only the exact collection prefix uses the hosted sprite cache', async () => {
    const sw = worker();
    for (const url of ['https://sprites.pkmds.app/other/image.png',
        'https://sprites.pkmds.app.evil.example/pokemon/25.png',
        'https://raw.githubusercontent.com/owner/repo/main/image.png']) {
        await sw.get(url);
        await sw.get(url);
    }
    assert.equal(sw.requests(), 6);
    assert.equal(sw.stores.has('pkmds-sprites-v2'), false);
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
