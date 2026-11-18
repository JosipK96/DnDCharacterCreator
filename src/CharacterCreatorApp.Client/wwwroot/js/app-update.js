// Tells the open app when a release's new service worker is waiting, and lets the player switch
// to it: the worker takes over on SKIP_WAITING, then the page reloads onto the new files.
window.appUpdate = {
    listen: async (dotNetRef) => {
        if (!window.swRegistration) {
            return;
        }

        const registration = await window.swRegistration;
        // No controller means this is the first install, which is not an update
        const signal = () => {
            if (registration.waiting && navigator.serviceWorker.controller) {
                dotNetRef.invokeMethodAsync('UpdateReady');
            }
        };

        signal();
        registration.addEventListener('updatefound', () => {
            registration.installing?.addEventListener('statechange', signal);
        });

        // A launch from the home screen may resume the old page without a navigation, so look
        // for a release whenever the app comes back to the front
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible') {
                registration.update().catch(() => { });
            }
        });
    },

    apply: async () => {
        const registration = await window.swRegistration;
        navigator.serviceWorker.addEventListener('controllerchange', () => window.location.reload(), { once: true });
        registration.waiting?.postMessage('SKIP_WAITING');
    },
};
