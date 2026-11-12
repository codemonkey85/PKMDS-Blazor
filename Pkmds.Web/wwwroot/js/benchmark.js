// Used only by the opt-in application benchmark, never by the normal Bank UI.
export async function requireIsolatedContext() {
    if (globalThis.pkmdsBenchmarkIsolation !== true || navigator.webdriver !== true ||
        !['127.0.0.1', 'localhost', '[::1]'].includes(location.hostname)) {
        throw new Error('Application benchmarks require the local Playwright harness and a fresh context.');
    }
    if (typeof indexedDB.databases !== 'function' || (await indexedDB.databases()).length !== 0) {
        throw new Error('Application benchmarks refuse an existing IndexedDB database.');
    }
}

export async function fingerprint(bytes) {
    const digest = await crypto.subtle.digest('SHA-256', bytes);
    return Array.from(new Uint8Array(digest), value => value.toString(16).padStart(2, '0')).join('');
}

let sampler;
export function startSampling() {
    if (sampler) throw new Error('A benchmark sampler is already active.');
    const period = 16;
    const state = { maxDelayMs: 0, samples: 0, next: performance.now() + period };
    state.timer = setInterval(() => {
        const now = performance.now();
        state.maxDelayMs = Math.max(state.maxDelayMs, now - state.next);
        state.samples++;
        state.next = now + period;
    }, period);
    sampler = state;
}

export function stopSampling() {
    if (!sampler) throw new Error('No benchmark sampler is active.');
    const state = sampler;
    clearInterval(state.timer);
    sampler = undefined;
    return JSON.stringify({
        MaxDelayMs: Math.max(state.maxDelayMs, performance.now() - state.next, 0),
        Samples: state.samples,
    });
}
