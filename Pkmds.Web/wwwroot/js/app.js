// Service worker registration
if ('serviceWorker' in navigator) {
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

// Listen for update events and forward to Blazor
window.addUpdateListener = () => {
    window.addEventListener('updateAvailable', () => {
        DotNet.invokeMethodAsync('Pkmds.Web', 'ShowUpdateMessage');
    });
};

