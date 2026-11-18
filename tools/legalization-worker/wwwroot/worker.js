// Adapted from the installed .NET 11 blazorwebworker template. A separate
// Mono runtime runs the same source-linked engine as the main-thread control.
// A classic bootstrap can load the app's unmodified UMD crypto-js bundle;
// dotnet.js is still loaded as an ES module with dynamic import below.
importScripts('./crypto-js.min.js', './crypto.js');
let methods;
self.addEventListener('message', async ({ data }) => {
    try {
        if (data.type === 'init') {
            const { dotnet } = await import(data.dotnetUrl);
            const runtime = await dotnet.create();
            const exports = await runtime.getAssemblyExports('Pkmds.WorkerPrototype');
            methods = exports.Pkmds.WorkerPrototype.WorkerMethods;
            methods.InitializeCrypto();
            self.postMessage({ type: 'ready' });
            return;
        }
        if (!methods) throw new Error('Worker is not initialized');
        if (!Object.hasOwn(methods, data.method) || typeof methods[data.method] !== 'function') {
            throw new Error('Unknown worker method');
        }
        self.postMessage({ type: 'started', id: data.id });
        const result = await methods[data.method](...data.args);
        self.postMessage({ type: 'result', id: data.id, result });
    } catch (error) {
        self.postMessage({ type: data.type === 'init' ? 'ready' : 'result', id: data.id,
            error: error?.message ?? String(error) });
    }
});
