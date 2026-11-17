import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const root = new URL('../../', import.meta.url);
const source = readFileSync(new URL('Pkmds.Web/wwwroot/js/browser-support.boot.js', root), 'utf8');
const index = readFileSync(new URL('Pkmds.Web/wwwroot/index.html', root), 'utf8');
const boot = [...index.matchAll(/<script>([\s\S]*?)<\/script>/g)].at(-1)[1];
const fallback = readFileSync(new URL('Pkmds.Web/wwwroot/BrowserNotSupported.html', root), 'utf8');
const fallbackScript = [...fallback.matchAll(/<script>([\s\S]*?)<\/script>/g)].at(-1)[1];

function fixture({exceptions = true, simd = true, wasm = true, blocked = false,
    requirements = {exceptionHandling: true, simd: true}, helper = true} = {}) {
    let starts = 0;
    let redirect;
    let bootOptions;
    const probes = [];
    const context = {
        URL, Uint8Array,
        document: {baseURI: 'https://example.test/pkmds/'},
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
        assert.equal(subject.redirect(), `https://example.test/pkmds/BrowserNotSupported.html?reason=${reason}`);
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
        URL, window: {location: {href: `https://example.test/BrowserNotSupported.html?reason=${encodeURIComponent(reason)}`}},
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

test('fallback explains the detected feature and ignores arbitrary query text', () => {
    assert.match(renderFallback('exceptions')['support-detail'].textContent, /exception handling/);
    assert.match(renderFallback('simd')['support-detail'].textContent, /SIMD/);
    for (const reason of ['<script>alert(1)</script>', '__proto__', 'toString']) {
        assert.equal(renderFallback(reason)['support-detail'].textContent, '');
    }
});
