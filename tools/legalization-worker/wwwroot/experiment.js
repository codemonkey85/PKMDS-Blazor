import { WorkerClient } from './worker-client.js';

const output = document.querySelector('#result');
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));
const assert = (condition, message) => { if (!condition) throw new Error(message); };
const clock = () => performance.now();

async function observe(operation) {
    const intervals = [];
    const frames = [];
    let last = clock();
    let lastFrame;
    let frame;
    const tick = now => {
        if (lastFrame !== undefined) frames.push(now - lastFrame);
        lastFrame = now;
        frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    const timer = setInterval(() => {
        const now = clock();
        intervals.push(now - last);
        last = now;
    }, 16);
    await delay(50);
    const start = clock();
    try {
        const value = await operation();
        const operationMs = clock() - start;
        await delay(50);
        return { value, operationMs, timerIntervalsMs: intervals, maxTimerIntervalMs: Math.max(0, ...intervals),
            frameIntervalsMs: frames, maxFrameIntervalMs: Math.max(0, ...frames) };
    } finally {
        clearInterval(timer);
        cancelAnimationFrame(frame);
    }
}

async function expectRejection(operation, text) {
    try {
        await operation();
    } catch (error) {
        assert(error.message.includes(text), `Expected '${text}', received '${error.message}'`);
        return error.message;
    }
    throw new Error(`Expected rejection: ${text}`);
}

async function digest(text) {
    const bytes = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text));
    return Array.from(new Uint8Array(bytes), n => n.toString(16).padStart(2, '0')).join('');
}

async function run() {
    // No storage APIs, user saves or live editor state. Refuse ordinary navigation
    // before loading .NET or constructing any worker.
    assert(['127.0.0.1', 'localhost', '[::1]'].includes(location.hostname) && navigator.webdriver &&
        new URLSearchParams(location.search).get('worker-prototype') === '1',
    'This experiment requires the automated loopback harness');
    const dotnetUrl = import.meta.resolve(new URL('_framework/dotnet.js', document.baseURI).href);
    const mainStart = clock();
    const { dotnet } = await import(dotnetUrl);
    const runtime = await dotnet.create();
    const exports = await runtime.getAssemblyExports('Pkmds.WorkerPrototype');
    const main = exports.Pkmds.WorkerPrototype.WorkerMethods;
    main.InitializeCrypto();
    const mainStartupMs = clock() - mainStart;
    const mainCrypto = JSON.parse(main.CheckCrypto());
    let client;
    try {
        const mainMemoryBeforeWorker = JSON.parse(main.MemorySnapshot());
        const workerStart = clock();
        client = await WorkerClient.create(dotnetUrl);
        const workerStartupMs = clock() - workerStart;
        const workerCrypto = JSON.parse(await client.invoke('CheckCrypto'));
        const workerMemoryBeforeGeneration = JSON.parse(await client.invoke('MemorySnapshot'));
        assert(JSON.stringify(mainCrypto) === JSON.stringify(workerCrypto), 'Main and worker crypto checks differ');
        const cases = [];
        for (const emptySet of [true, false]) {
            const request = main.CreateRequest(emptySet);
            const requestHash = await digest(request);
            const serializedBytes = new TextEncoder().encode(request).length;
            const outputs = {};
            const workerFirst = new URLSearchParams(location.search).get('worker-first') === '1';
            for (const mode of workerFirst ? ['worker', 'main'] : ['main', 'worker']) {
                const observation = await observe(() => mode === 'main'
                    ? main.Generate(request)
                    : client.invoke('Generate', [request]));
                const reply = observation.value;
                const summary = JSON.parse(main.Validate(request, reply));
                assert(await digest(request) === requestHash, 'Caller request changed');
                delete observation.value;
                outputs[mode] = { ...observation, summary,
                    resultBase64Characters: JSON.parse(reply).PokemonBytes.length };
            }
            assert(JSON.stringify(outputs.main.summary) === JSON.stringify(outputs.worker.summary),
                'Main and worker requested semantics differ');
            cases.push({ emptySet, requestHash, serializedBytes, ...outputs });
        }
        const workerMemoryAfterGeneration = JSON.parse(await client.invoke('MemorySnapshot'));
        const mainMemoryAfterGeneration = JSON.parse(main.MemorySnapshot());

        const controlBlock = await observe(() => main.BlockForMilliseconds(500));
        const workerBlock = await observe(() => client.invoke('BlockForMilliseconds', [500]));
        // A method exception rejects only that operation; the same worker remains usable.
        const remoteError = await expectRejection(() => client.invoke('Generate', ['{']), '');
        const recovery = JSON.parse(await client.invoke('MemorySnapshot'));
        let concurrentRejected = false;
        const busy = client.invoke('BlockForMilliseconds', [100], { onStarted: () => {
            // Store the promise now, and inspect it before continuing.
            concurrentRejected = expectRejection(() => client.invoke('MemorySnapshot'), 'already busy');
        } });
        await busy;
        assert(concurrentRejected && await concurrentRejected, 'Concurrent request was not rejected');

        const controller = new AbortController();
        let cancelStarted = false;
        let cancelAt;
        let abortTimer;
        const cancelRequest = client.invoke('BlockForMilliseconds', [2000], {
            signal: controller.signal,
            onStarted: () => {
                cancelStarted = true;
                abortTimer = setTimeout(() => { cancelAt = clock(); controller.abort(); }, 50);
            },
        });
        try {
            await expectRejection(() => cancelRequest, 'abort');
        } finally {
            clearTimeout(abortTimer);
        }
        const cancelObservedMs = clock() - cancelAt;
        assert(cancelStarted, 'Cancellation did not reach running CPU work');
        await expectRejection(() => client.invoke('MemorySnapshot'), 'terminated');

        client = await WorkerClient.create(dotnetUrl);
        let timeoutStarted = false;
        const timeoutStart = clock();
        await expectRejection(() => client.invoke('BlockForMilliseconds', [2000], {
            timeoutMs: 150, onStarted: () => { timeoutStarted = true; },
        }), 'timed out');
        const timeoutObservedMs = clock() - timeoutStart;
        assert(timeoutStarted, 'Timeout did not reach running CPU work');
        await expectRejection(() => client.invoke('MemorySnapshot'), 'terminated');

        client = await WorkerClient.create(dotnetUrl);
        const request = main.CreateRequest(true);
        main.Validate(request, await client.invoke('Generate', [request]));
        client.terminate();
        const preAborted = new AbortController();
        preAborted.abort();
        await expectRejection(() => WorkerClient.create(dotnetUrl, { signal: preAborted.signal }), 'abort');

        return {
            mainStartupMs, workerStartupMs, crypto: { main: mainCrypto, worker: workerCrypto }, cases,
            memory: { mainMemoryBeforeWorker, mainMemoryAfterGeneration,
                workerMemoryBeforeGeneration, workerMemoryAfterGeneration },
            syntheticBlocking: { main: controlBlock, worker: workerBlock },
            lifecycle: { remoteError, recoveredAfterError: !!recovery, concurrentRejected: true,
                cancelStarted, cancelObservedMs, timeoutStarted, timeoutObservedMs,
                recreatedAfterTermination: true, preAbortedCreationRejected: true },
        };
    } finally {
        client?.terminate();
    }
}

try {
    window.workerPrototypeResult = { ok: true, ...await run() };
} catch (error) {
    window.workerPrototypeResult = { ok: false, error: error.stack ?? String(error) };
}
output.textContent = JSON.stringify(window.workerPrototypeResult, null, 2);
