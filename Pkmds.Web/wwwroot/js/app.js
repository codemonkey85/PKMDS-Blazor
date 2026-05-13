// Detect embedded host mode from the URL query string. This runs before Blazor
// boots, so we can't ask IHostService — plain JS only. Mirrors the parsing in
// Pkmds.Rcl/Services/HostService.cs (case-insensitive key, empty value treated
// as not embedded). Skipping SW registration in embed contexts avoids caching
// app shell assets inside a host-bundled WKWebView, which would compete with
// the host's bundle and make host-bundled updates invisible.
function pkmdsIsEmbedded() {
    try {
        const params = new URLSearchParams(window.location.search);
        for (const [key, value] of params.entries()) {
            if (key.toLowerCase() === 'host' && value && value.trim()) {
                return true;
            }
        }
    } catch (e) {
        // URLSearchParams unavailable or malformed query — fall back to standalone behaviour.
    }
    return false;
}

// Service worker registration
if (pkmdsIsEmbedded()) {
    console.info('Service worker registration skipped: embedded host mode.');
    window._swRegistrationPromise = Promise.resolve(null);
} else if ('serviceWorker' in navigator) {
    window._swRegistrationPromise = navigator.serviceWorker.register('service-worker.js', {updateViaCache: 'none'}).then(registration => {
        console.info('Service worker registered, scope:', registration.scope);
        setInterval(() => registration.update().catch(err => {
            // Safari may throw "newestWorker is null" — this is benign
            if (!(err.name === 'InvalidStateError' || (err.message && err.message.includes('newestWorker is null')))) {
                console.warn('Periodic registration.update() failed:', err);
            }
        }), 60 * 60 * 1000); // check for updates every hour
        registration.onupdatefound = () => {
            const installingWorker = registration.installing;
            installingWorker.onstatechange = () => {
                if (installingWorker.state === 'installed' && navigator.serviceWorker.controller) {
                    // Notify Blazor about the update
                    window.dispatchEvent(new CustomEvent('updateAvailable'));
                }
            };
        };
        return registration;
    }).catch(err => {
        console.error('Service worker registration failed:', err);
        return null;
    });
} else {
    console.warn('Service workers are not supported in this browser.');
    window._swRegistrationPromise = Promise.resolve(null);
}

// Embedded PoC polyfill — active only when ?host=poc.
//
// Routes PKMDS outbound messages (ready, saveExport) to the parent page via
// window.parent.postMessage, and handles inbound method calls (loadSave,
// requestExport) arriving as postMessages from the parent.
//
// This makes the standalone tools/embedded-host-poc/index.html page work
// cross-origin without requiring same-origin iframe access or Xcode.
// All other host values and standalone mode are completely unaffected.
(function () {
    var params;
    try { params = new URLSearchParams(window.location.search); } catch (e) { return; }
    if (params.get('host') !== 'poc') return;

    // Polyfill window.webkit.messageHandlers.pkmds to route outbound messages
    // to the parent page. host.js reads window.webkit at call time (not at
    // definition time), so this is safe to set even after host.js has loaded.
    window.webkit = window.webkit || {};
    window.webkit.messageHandlers = window.webkit.messageHandlers || {};
    window.webkit.messageHandlers.pkmds = {
        postMessage: function (msg) {
            window.parent.postMessage(msg, '*');
        }
    };

    // Handle inbound method calls from the parent page.
    // Protocol: parent sends { type: 'pkmds-host-call', method, args, id }
    //           iframe responds { type: 'pkmds-host-result', id, result }
    // The parent should only call loadSave / requestExport after receiving the
    // 'ready' outbound message, at which point window.PKMDS.host is available.
    window.addEventListener('message', function (event) {
        var data = event.data;
        if (!data || data.type !== 'pkmds-host-call') return;
        var method = data.method;
        var args = data.args || [];
        var id = data.id;
        var src = event.source || window.parent;
        Promise.resolve()
            .then(function () { return window.PKMDS.host[method].apply(window.PKMDS.host, args); })
            .then(function (result) { src.postMessage({ type: 'pkmds-host-result', id: id, result: result }, '*'); })
            .catch(function (err) { src.postMessage({ type: 'pkmds-host-result', id: id, result: false, error: String(err) }, '*'); });
    });
})();

// Bench page helper — called from Benchmark.razor instead of eval to avoid unsafe-eval.
window.pkmdsGetUserAgent = () => navigator.userAgent;

// Listen for update events and forward to Blazor
window.addUpdateListener = () => {
    window.addEventListener('updateAvailable', () => {
        DotNet.invokeMethodAsync('Pkmds.Web', 'ShowUpdateMessage');
    });
};

// Proactively check for a service worker update.
// Returns: 'found' (update ready), 'none' (up to date), 'no-sw' (SW unavailable), 'error' (check/install failed)
window.checkForUpdates = async () => {
    const registration = await window._swRegistrationPromise;
    if (!registration) return 'no-sw';

    // A waiting worker was already downloaded but not yet activated — notify immediately.
    if (registration.waiting) {
        window.dispatchEvent(new CustomEvent('updateAvailable'));
        return 'found';
    }

    let updated;
    try {
        updated = await registration.update();
    } catch (err) {
        if (!(err.name === 'InvalidStateError' || (err.message && err.message.includes('newestWorker is null')))) {
            console.warn('Manual update check failed:', err);
        }
        return 'error';
    }

    if (updated.waiting) {
        window.dispatchEvent(new CustomEvent('updateAvailable'));
        return 'found';
    }

    if (!updated.installing) {
        return 'none';
    }

    // New SW is installing — wait for it to fully succeed or fail before returning.
    // This prevents a silent no-feedback state when the install errors (e.g. SRI hash mismatch
    // during a fresh deployment before CDN has fully propagated the new asset files).
    const installing = updated.installing;
    return new Promise((resolve) => {
        const timeoutId = setTimeout(() => resolve('error'), 30000);
        installing.addEventListener('statechange', function () {
            if (installing.state === 'installed') {
                clearTimeout(timeoutId);
                window.dispatchEvent(new CustomEvent('updateAvailable'));
                resolve('found');
            } else if (installing.state === 'redundant') {
                clearTimeout(timeoutId);
                resolve('error');
            }
        });
    });
};

