import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import test from 'node:test';
import vm from 'node:vm';

const source = readFileSync(new URL('../../Pkmds.Web/wwwroot/js/benchmark.js', import.meta.url), 'utf8');

function fixture({marker = true, webdriver = true, hostname = '127.0.0.1', databases = []} = {}) {
    let reads = 0;
    let now = 0;
    let callback;
    let cleared = false;
    const context = {
        pkmdsBenchmarkIsolation: marker,
        navigator: {webdriver}, location: {hostname},
        indexedDB: {databases: async () => { reads++; return databases; }},
        performance: {now: () => now},
        setInterval: action => { callback = action; return 1; },
        clearInterval: id => { assert.equal(id, 1); cleared = true; },
    };
    const api = vm.runInNewContext(source.replace(/^export /gm, '') + '\n({requireIsolatedContext, startSampling, stopSampling})', context);
    return {api, reads: () => reads, advance: value => { now = value; }, tick: () => callback(), cleared: () => cleared};
}

for (const options of [{marker: false}, {webdriver: false}, {hostname: 'pkmds.app'}]) {
    test(`rejects an unsafe context before even enumerating databases: ${JSON.stringify(options)}`, async () => {
        const subject = fixture(options);
        await assert.rejects(subject.api.requireIsolatedContext(), /require the local Playwright harness/);
        assert.equal(subject.reads(), 0);
    });
}

test('refuses existing Bank or unrelated IndexedDB data without modifying it', async () => {
    const databases = Object.freeze([Object.freeze({name: 'pkmds-bank', version: 1})]);
    const subject = fixture({databases});
    await assert.rejects(subject.api.requireIsolatedContext(), /refuse an existing IndexedDB database/);
    assert.equal(subject.reads(), 1);
    assert.deepEqual(databases, [{name: 'pkmds-bank', version: 1}]);
});

test('accepts a fresh automated loopback context', async () => {
    const subject = fixture();
    await subject.api.requireIsolatedContext();
    assert.equal(subject.reads(), 1);
});

test('records a blocked event loop even when no timer callback could run', () => {
    const subject = fixture();
    subject.api.startSampling();
    subject.advance(95);
    assert.deepEqual(JSON.parse(subject.api.stopSampling()), {MaxDelayMs: 79, Samples: 0});
    assert.equal(subject.cleared(), true);
});

test('records lateness relative to the next deadline and releases the sampler', () => {
    const subject = fixture();
    subject.api.startSampling();
    subject.advance(21);
    subject.tick();
    subject.advance(50);
    assert.deepEqual(JSON.parse(subject.api.stopSampling()), {MaxDelayMs: 13, Samples: 1});
    assert.doesNotThrow(() => subject.api.startSampling());
    subject.api.stopSampling();
});
