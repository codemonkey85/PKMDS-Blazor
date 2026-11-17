import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const root = new URL('../../', import.meta.url);
const source = readFileSync(new URL('Pkmds.Web/wwwroot/js/browser-support.boot.js', root), 'utf8');
const index = readFileSync(new URL('Pkmds.Web/wwwroot/index.html', root), 'utf8');
const boot = lastScriptBody(index);
const fallback = readFileSync(new URL('Pkmds.Web/wwwroot/BrowserNotSupported.html', root), 'utf8');
const fallbackScript = lastScriptBody(fallback);
const workerSource = readFileSync(new URL('Pkmds.Web/wwwroot/service-worker.published.js', root), 'utf8');

// Both owned fixtures end with the inline script exercised below.
function lastScriptBody(html) {
    const match = [...html.matchAll(/<script\b[^>]*>([\s\S]*?)<\/script\b[^>]*>/gi)].at(-1);
    assert.ok(match, 'Expected a script in the HTML fixture');
    return match[1];
}

test('fixture extraction handles mixed-case tags, whitespace, and closing-tag attributes', () => {
    const html = '<script>first()</script><ScRiPt \n>last()</sCrIpT data-extra="accepted">';
    assert.equal(lastScriptBody(html), 'last()');
    assert.throws(() => lastScriptBody('<p>No script</p>'), /Expected a script/);
});

function fixture({exceptions = true, simd = true, wasm = true, blocked = false,
    requirements = {exceptionHandling: true, simd: true}, helper = true,
    baseURI = 'https://example.test/pkmds/'} = {}) {
    let starts = 0;
    let redirect;
    let bootOptions;
    const probes = [];
    const context = {
        URL, Uint8Array,
        document: {baseURI},
        location: {
            href: 'https://example.test/pkmds/bench?old=1#fragment',
            replace: value => { redirect = value; }
        },
        pkmdsBrowserRequirements: requirements,
        WebAssembly: wasm ? {
            instantiate() {},
            Module: class { constructor(bytes) {
                if (blocked) throw new Error('Blocked by browser policy');
                assert.ok(WebAssembly.validate(bytes));
            } },
            Instance: class {},
            validate: bytes => {
                probes.push(bytes);
                return bytes.includes(31) ? exceptions : simd;
            }
        } : undefined,
        Blazor: {start: options => {
            starts++;
            bootOptions = options;
            return Promise.resolve();
        }},
        markAppBootSucceeded() {}
    };
    context.window = context;
    // Both successful and failed startup must leave user storage/cache alone.
    for (const name of ['indexedDB', 'localStorage', 'caches', 'navigator']) {
        Object.defineProperty(context, name, {get() { throw new Error(`Unexpected access to ${name}`); }});
    }
    vm.createContext(context);
    if (helper) vm.runInContext(source, context);
    return {
        check: () => context.pkmdsCheckBrowser(requirements),
        boot: () => vm.runInContext(boot, context),
        starts: () => starts, redirect: () => redirect, probes,
        options: () => bootOptions
    };
}

test('enabled capabilities allow the build to start', () => {
    const subject = fixture({requirements: {exceptionHandling: true, simd: true}});
    subject.boot();
    assert.equal(subject.starts(), 1);
    assert.equal(subject.redirect(), undefined);
    assert.equal(subject.probes.length, 2);
    // Guard the final-EH probe against replacement with legacy try/catch.
    assert.ok(subject.probes[0].includes(0x69), 'exnref local');
    assert.ok(subject.probes[0].includes(0x1f), 'try_table opcode');
    assert.equal(subject.options().loadBootResource('dotnetjs', '', '', ''), undefined);
});

// Older CI Node versions may lack final EH themselves. The simulated browser
// tests above/below still run there; validate the actual bytecode on Node 26+.
test('probe modules validate in a host with final exception handling', {
    skip: Number(process.versions.node.split('.')[0]) < 26 ? 'Requires Node 26+ final EH' : false
}, () => {
    const subject = fixture({requirements: {exceptionHandling: true, simd: true}});
    subject.check();
    for (const bytes of subject.probes) assert.equal(WebAssembly.validate(bytes), true);
});

test('a build with SIMD disabled does not require or probe SIMD', () => {
    const subject = fixture({simd: false, requirements: {exceptionHandling: true, simd: false}});
    subject.boot();
    assert.equal(subject.starts(), 1);
    assert.equal(subject.probes.length, 1);
});

test('exception handling follows the resolved build setting too', () => {
    const subject = fixture({exceptions: false, requirements: {exceptionHandling: false, simd: false}});
    subject.boot();
    assert.equal(subject.starts(), 1);
    assert.equal(subject.probes.length, 0);
});

for (const [reason, options] of [
    ['webassembly', {wasm: false}],
    ['webassembly', {blocked: true}],
    // Models a browser that can validate SIMD/legacy EH but lacks final EH.
    ['exceptions', {exceptions: false}],
    ['simd', {simd: false, requirements: {exceptionHandling: true, simd: true}}],
    ['configuration', {requirements: null}],
    ['configuration', {requirements: {exceptionHandling: 'true', simd: false}}],
    ['configuration', {requirements: {exceptionHandling: true}}],
    ['configuration', {helper: false}]
]) {
    test(`blocks startup and uses a base-relative fallback: ${reason} ${JSON.stringify(options)}`, () => {
        const subject = fixture(options);
        subject.boot();
        assert.equal(subject.starts(), 0);
        assert.equal(subject.redirect(), `https://example.test/pkmds/BrowserNotSupported.html#reason=${reason}`);
    });
}

test('a throwing feature validator fails safely before startup', () => {
    const context = {WebAssembly: {
        instantiate() {}, Module: class {}, Instance: class {},
        validate() { throw new Error('Feature validation blocked'); }
    }};
    context.window = context;
    vm.runInNewContext(source, context);
    assert.equal(context.pkmdsCheckBrowser({exceptionHandling: true, simd: false}), 'exceptions');
});

function renderFallback(reason) {
    const elements = Object.fromEntries(['support-title', 'support-detail', 'browser-guidance']
        .map(id => [id, {textContent: '', hidden: false}]));
    vm.runInNewContext(fallbackScript, {
        URL, URLSearchParams,
        window: {location: {href: `https://example.test/BrowserNotSupported.html#reason=${encodeURIComponent(reason)}`}},
        document: {getElementById: id => elements[id]}
    });
    return elements;
}

test('missing build metadata gets reload guidance instead of an outdated-browser claim', () => {
    const elements = renderFallback('configuration');
    assert.match(elements['support-title'].textContent, /could not check/);
    assert.match(elements['support-detail'].textContent, /reload steps/);
    assert.equal(elements['browser-guidance'].hidden, true);
});

test('fallback explains the detected feature and ignores arbitrary fragment text', () => {
    assert.match(renderFallback('exceptions')['support-detail'].textContent, /exception handling/);
    assert.match(renderFallback('simd')['support-detail'].textContent, /SIMD/);
    for (const reason of ['<script>alert(1)</script>', '__proto__', 'toString']) {
        assert.equal(renderFallback(reason)['support-detail'].textContent, '');
    }
});

async function controlledNavigation(url, offline) {
    const origin = 'https://example.test';
    const cache = new Map([
        [new URL('index.html', origin).href, new Response(index)],
        [new URL('BrowserNotSupported.html', origin).href, new Response(fallback)]
    ]);
    let networkRequests = 0;
    const context = vm.createContext({
        URL, Request, Response, console,
        self: {origin, assetsManifest: {version: 'test', assets: [
            {url: 'index.html'}, {url: 'BrowserNotSupported.html'}
        ]}, importScripts() {}, addEventListener() {}},
        caches: {open: async () => ({
            match: async request => {
                const key = new URL(typeof request === 'string' ? request : request.url, origin);
                key.hash = ''; // Cache Storage ignores fragments, even when request.url retains one.
                return cache.get(key.href)?.clone();
            }
        })},
        fetch: async () => {
            networkRequests++;
            if (offline) throw new TypeError('Offline');
            return new Response('Network fallback');
        }
    });
    vm.runInContext(workerSource, context);
    // Chromium retains the fragment on the worker's navigation request.
    const response = await context.onFetch({request: {url, method: 'GET', mode: 'navigate'}});
    return {body: await response.text(), networkRequests};
}

for (const offline of [false, true]) {
    for (const [reason, options] of [
        ['webassembly', {wasm: false}], ['exceptions', {exceptions: false}],
        ['simd', {simd: false}], ['configuration', {requirements: null}]
    ]) {
        test(`controlled worker serves the cached fallback for ${reason}, offline=${offline}`, async () => {
            const subject = fixture({...options, baseURI: 'https://example.test/'});
            subject.boot();
            const result = await controlledNavigation(subject.redirect(), offline);
            assert.equal(result.body.includes('id="support-title"'), true, 'Expected the fallback, not the app shell');
            assert.equal(result.networkRequests, 0);
            assert.equal(subject.starts(), 0);
        });
    }
}
