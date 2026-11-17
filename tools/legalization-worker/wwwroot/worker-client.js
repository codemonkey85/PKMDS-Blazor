// One operation at a time: PKHeX ParseSettings/GameInfo are per-runtime globals.
// Aborting or timing out terminates the worker, including synchronous work.
// Recreate it explicitly before the next request; no automatic replay of work.
export class WorkerClient {
    #worker;
    #pending;
    #nextId = 0;

    constructor(worker) {
        this.#worker = worker;
        worker.addEventListener('message', ({ data }) => {
            const pending = this.#pending;
            if (!pending || (data.type === 'ready' ? !pending.initializing : data.id !== pending.id)) return;
            if (data.type === 'started') {
                pending.onStarted?.();
                return;
            }
            data.error ? pending.reject(new Error(data.error)) : pending.resolve(data.result);
        });
        worker.addEventListener('error', event => this.terminate(new Error(event.message || 'Worker error')));
        worker.addEventListener('messageerror', () => this.terminate(new Error('Unreadable worker reply')));
    }

    static async create(dotnetUrl, { signal, timeoutMs = 60000 } = {}) {
        if (signal?.aborted) throw signal.reason ?? new DOMException('Aborted', 'AbortError');
        const client = new WorkerClient(new Worker(new URL('./worker.js', import.meta.url)));
        try {
            await client.#send({ type: 'init', dotnetUrl }, { signal, timeoutMs });
            return client;
        } catch (error) {
            client.terminate(error);
            throw error;
        }
    }

    invoke(method, args = [], options = {}) {
        return this.#send({ method, args }, options);
    }

    #send(message, { signal, timeoutMs = 30000, onStarted } = {}) {
        if (!this.#worker) return Promise.reject(new Error('Worker terminated'));
        if (this.#pending) return Promise.reject(new Error('Worker already busy'));
        if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) return Promise.reject(new Error('Invalid timeout'));
        if (signal?.aborted) {
            this.terminate();
            return Promise.reject(signal.reason ?? new DOMException('Aborted', 'AbortError'));
        }
        return new Promise((resolve, reject) => {
            const id = ++this.#nextId;
            const abort = () => this.terminate(signal.reason ?? new DOMException('Aborted', 'AbortError'));
            const timer = setTimeout(() => this.terminate(new Error('Worker timed out')), timeoutMs);
            const finish = callback => value => {
                clearTimeout(timer);
                signal?.removeEventListener('abort', abort);
                this.#pending = undefined;
                callback(value);
            };
            this.#pending = { id, initializing: message.type === 'init',
                resolve: finish(resolve), reject: finish(reject), onStarted };
            signal?.addEventListener('abort', abort, { once: true });
            try {
                this.#worker.postMessage({ ...message, id });
            } catch (error) {
                this.terminate(error);
            }
        });
    }

    terminate(reason = new Error('Worker terminated')) {
        this.#worker?.terminate();
        this.#worker = undefined;
        this.#pending?.reject(reason);
    }
}
