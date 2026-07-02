(function () {
    // Detect embedded host mode from the URL query string. When embedded, the
    // host owns appearance and we should always follow prefers-color-scheme
    // rather than honouring any standalone-mode theme persisted in localStorage.
    // Mirrors the parsing in HostService.cs and app.js (case-insensitive key,
    // empty value treated as not embedded).
    let isEmbedded = false;
    try {
        const params = new URLSearchParams(window.location.search);
        for (const [key, value] of params.entries()) {
            if (key.toLowerCase() === 'host' && value && value.trim()) {
                isEmbedded = true;
                break;
            }
        }
    } catch (_) {
    }

    let stored = null;
    if (!isEmbedded) {
        try {
            stored = localStorage.getItem('pkmds_theme');
        } catch (_) {
        }
    }
    const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    const theme = stored || (prefersDark ? 'dark' : 'light');
    document.documentElement.setAttribute('data-theme', theme);
})();

window.setAppTheme = function (theme, themeColor) {
    document.documentElement.setAttribute('data-theme', theme);

    // Keep the PWA window-chrome color derived from the C# theme (AppTheme.PwaThemeColor,
    // passed via interop) instead of duplicating the hex here. The static value in
    // index.html is only the pre-boot placeholder.
    if (themeColor) {
        const meta = document.querySelector('meta[name="theme-color"]');
        if (meta) {
            meta.setAttribute('content', themeColor);
        }
    }
};
