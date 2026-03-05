(function () {
    var stored = null;
    try {
        stored = localStorage.getItem('pkmds_theme');
    } catch (_) {
    }
    var prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    var theme = stored || (prefersDark ? 'dark' : 'light');
    document.documentElement.setAttribute('data-theme', theme);
})();

window.setAppTheme = function (theme) {
    document.documentElement.setAttribute('data-theme', theme);
};
