// Force a clean reload: unregister all service workers and delete every cache,
// then hard-reload the page. Used by the "Clear App Cache" button in Settings as
// an escape hatch when a deploy's service-worker update fails to invalidate the
// previous cache (stale JSON data, old WASM, etc.).
window.clearAppCacheAndReload = async function () {
    try {
        if ('serviceWorker' in navigator) {
            const registrations = await navigator.serviceWorker.getRegistrations();
            await Promise.all(registrations.map(r => r.unregister()));
        }
        if ('caches' in window) {
            const keys = await caches.keys();
            await Promise.all(keys.map(k => caches.delete(k)));
        }
    } catch (err) {
        console.error('clearAppCacheAndReload: cleanup failed', err);
    } finally {
        // location.reload(true) was removed from the spec; use a cache-busting
        // replace() to force a fresh page fetch.
        const bust = Date.now();
        const sep = window.location.href.includes('?') ? '&' : '?';
        window.location.replace(`${window.location.href.replace(/[?&]_cb=\d+/g, '')}${sep}_cb=${bust}`);
    }
};

// Recover once from an online startup failure caused by a stale or mixed app cache. The marker
// survives the repair reload so a genuine network problem cannot create a reload loop. A
// successful Blazor boot clears it for future deployments. This intentionally touches only
// service workers and Cache Storage; IndexedDB saves, backups, and the Pokémon Bank are retained.
window.tryAutomaticAppCacheRecovery = async function () {
    const recoveryKey = 'pkmds-app-cache-recovery-attempted';
    try {
        if (sessionStorage.getItem(recoveryKey) || !navigator.onLine) {
            return false;
        }

        // Prove the current deployment is reachable before discarding offline app assets.
        // service-worker.js is excluded from app caches and the query makes this probe unique.
        const probe = await fetch(`service-worker.js?_recovery=${Date.now()}`, {cache: 'no-store'});
        const contentType = probe.headers.get('Content-Type') || '';
        if (!probe.ok || !contentType.includes('javascript')) {
            return false;
        }

        sessionStorage.setItem(recoveryKey, 'true');
        await window.clearAppCacheAndReload();
        return true;
    } catch (err) {
        console.warn('tryAutomaticAppCacheRecovery: recovery probe failed', err);
        return false;
    }
};

window.markAppBootSucceeded = function () {
    window._pkmdsBootInProgress = false;
    try {
        sessionStorage.removeItem('pkmds-app-cache-recovery-attempted');
    } catch (_) {
        // Storage can be unavailable in hardened/private browsing modes.
    }
};
