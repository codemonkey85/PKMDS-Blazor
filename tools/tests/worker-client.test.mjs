import { test, afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { WorkerClient } from '../legalization-worker/wwwroot/worker-client.js';

const originalWorker = globalThis.Worker;
const workers = [];
class FakeWorker extends EventTarget {
    messages = [];
    terminated = false;
    constructor() { super(); workers.push(this); }
    postMessage(message) {
        this.messages.push(message);
        if (message.type === 'init') queueMicrotask(() => this.reply({ type: 'ready' }));
    }
    reply(data) { this.dispatchEvent(new MessageEvent('message', { data })); }
    terminate() { this.terminated = true; }
}

afterEach(() => { globalThis.Worker = originalWorker; workers.length = 0; });
async function ready() {
    globalThis.Worker = FakeWorker;
    const client = await WorkerClient.create('http://127.0.0.1/_framework/dotnet.js');
    return { client, worker: workers[0] };
}

test('matching replies release the single-flight slot; stale replies cannot complete the next request', async () => {
    const { client, worker } = await ready();
    try {
        const first = client.invoke('Generate');
        const id = worker.messages.at(-1).id;
        worker.reply({ type: 'result', id, result: 'one' });
        assert.equal(await first, 'one');
        let settled = false;
        const second = client.invoke('Generate').then(value => { settled = true; return value; });
        worker.reply({ type: 'result', id, result: 'stale' });
        await Promise.resolve();
        assert.equal(settled, false);
        worker.reply({ type: 'result', id: worker.messages.at(-1).id, result: 'two' });
        assert.equal(await second, 'two');
    } finally { client.terminate(); }
});

test('method failures preserve the worker, but concurrent requests cannot race PKHeX globals', async () => {
    const { client, worker } = await ready();
    try {
        const first = client.invoke('Generate');
        await assert.rejects(client.invoke('Generate'), /already busy/);
        worker.reply({ type: 'result', id: worker.messages.at(-1).id, error: 'bad snapshot' });
        await assert.rejects(first, /bad snapshot/);
        assert.equal(worker.terminated, false);
        const next = client.invoke('MemorySnapshot');
        worker.reply({ type: 'result', id: worker.messages.at(-1).id, result: 'ok' });
        assert.equal(await next, 'ok');
    } finally { client.terminate(); }
});

test('cancellation terminates running work and requires explicit recreation', async () => {
    const { client, worker } = await ready();
    const controller = new AbortController();
    let started = false;
    const pending = client.invoke('Generate', [], { signal: controller.signal,
        onStarted: () => { started = true; } });
    worker.reply({ type: 'started', id: worker.messages.at(-1).id });
    controller.abort();
    await assert.rejects(pending, /abort/i);
    assert.equal(started, true);
    assert.equal(worker.terminated, true);
    await assert.rejects(client.invoke('Generate'), /terminated/);
});

test('timeout before dispatch terminates the worker; a late start cannot restore it', async () => {
    const { client, worker } = await ready();
    let started = false;
    await assert.rejects(client.invoke('Generate', [], { timeoutMs: 5,
        onStarted: () => { started = true; } }), /timed out/);
    assert.equal(started, false);
    assert.equal(worker.terminated, true);
    worker.reply({ type: 'started', id: worker.messages.at(-1).id });
    assert.equal(started, false);
    await assert.rejects(client.invoke('Generate'), /terminated/);
});

test('pre-cancelled initialization never constructs or downloads a worker', async () => {
    globalThis.Worker = FakeWorker;
    const controller = new AbortController();
    controller.abort();
    await assert.rejects(WorkerClient.create('unused', { signal: controller.signal }), /abort/i);
    assert.equal(workers.length, 0);
});

test('cancellation during initialization terminates the worker before readiness', async () => {
    globalThis.Worker = class extends FakeWorker {
        postMessage(message) { this.messages.push(message); }
    };
    const controller = new AbortController();
    const initializing = WorkerClient.create('unused', { signal: controller.signal });
    controller.abort();
    await assert.rejects(initializing, /abort/i);
    assert.equal(workers[0].terminated, true);
});

test('worker errors and explicit disposal reject pending work', async () => {
    const { client, worker } = await ready();
    const pending = client.invoke('Generate');
    worker.dispatchEvent(new Event('messageerror'));
    await assert.rejects(pending, /Unreadable/);
    assert.equal(worker.terminated, true);
    client.terminate(); // idempotent
});
