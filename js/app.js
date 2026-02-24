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

// Check for service worker updates on demand
window.checkForUpdate = async () => {
    if (!('serviceWorker' in navigator)) {
        return {result: 'no-sw', detail: 'Service workers not supported by this browser.'};
    }
    try {
        // Use the stored registration promise from initial registration
        let registration = window._swRegistrationPromise
            ? await window._swRegistrationPromise
            : null;
        const fromStored = !!registration;
        // Fall back to getRegistration if the stored promise resolved to null
        if (!registration) {
            registration = await navigator.serviceWorker.getRegistration();
        }
        if (!registration) {
            return {
                result: 'no-sw',
                detail: fromStored
                    ? 'Registration promise resolved to null.'
                    : 'No stored registration and getRegistration() returned null.'
            };
        }
        // If a waiting worker already exists before we call update(), an update is pending
        if (registration.waiting) {
            return {result: 'update-found', detail: 'A waiting worker was already present.'};
        }
        // Remember the current active worker so we can detect if it actually changed
        const previousActive = registration.active;
        // Listen for the updatefound event, then call update()
        return await new Promise(resolve => {
            let resolved = false;
            const done = (result, detail) => {
                if (resolved) return;
                resolved = true;
                registration.removeEventListener('updatefound', onUpdateFound);
                resolve({result, detail: detail || ''});
            };
            const onUpdateFound = () => {
                const newWorker = registration.installing;
                if (!newWorker) {
                    done('no-update', 'updatefound fired but no installing worker.');
                    return;
                }
                // Wait for the new worker to finish installing
                newWorker.addEventListener('statechange', () => {
                    if (newWorker.state === 'installed') {
                        done('update-found', 'New worker installed and waiting.');
                    } else if (newWorker.state === 'activated') {
                        if (previousActive && registration.active === previousActive) {
                            done('no-update', 'Worker re-activated but is the same instance.');
                        } else {
                            done('update-found', 'New worker activated.');
                        }
                    } else if (newWorker.state === 'redundant') {
                        done('no-update', 'New worker became redundant.');
                    }
                });
            };
            registration.addEventListener('updatefound', onUpdateFound);
            registration.update().catch(err => {
                // Some browsers (e.g. Safari) throw "newestWorker is null" InvalidStateError
                // when calling update() and the SW is already active with no pending update.
                // This is not a real error — it means we're already up to date.
                if (err.name === 'InvalidStateError' || (err.message && err.message.includes('newestWorker is null'))) {
                    console.info('registration.update() threw InvalidStateError (newestWorker is null) — treating as no update available.');
                    done('no-update', 'Service worker is up to date (no pending update).');
                } else if (err.name === 'NetworkError') {
                    // Network errors during update check are treated as "no update available"
                    console.info('registration.update() failed with NetworkError — network may be unavailable.');
                    done('no-update', 'Network error during update check.');
                } else {
                    console.warn('registration.update() failed:', err);
                    done('error', 'update() threw: ' + err.message);
                }
            });
            setTimeout(() => done('no-update', 'Timed out after 8 seconds.'), 8000);
        });
    } catch (error) {
        console.warn('checkForUpdate error:', error);
        return {result: 'error', detail: error.message};
    }
};
