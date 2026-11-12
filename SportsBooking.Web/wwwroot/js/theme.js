(function () {
    const storageKey = 'sportsbooking-theme';
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)');
    let preference;
    try { preference = localStorage.getItem(storageKey); } catch (_) { /* Storage may be disabled. */ }
    if (preference !== 'light' && preference !== 'dark') preference = null;

    function applyTheme() {
        const dark = preference ? preference === 'dark' : systemTheme.matches;
        document.documentElement.dataset.theme = dark ? 'dark' : 'light';
        const button = document.querySelector('.theme-toggle');
        if (!button) return;
        button.setAttribute('aria-pressed', String(dark));
        button.setAttribute('aria-label', dark ? 'Светъл режим' : 'Тъмен режим');
        button.querySelector('[data-theme-icon]').textContent = dark ? '☀' : '☾';
        button.querySelector('[data-theme-label]').textContent = dark ? 'Светъл режим' : 'Тъмен режим';
    }

    // Apply before the stylesheet loads to avoid flashing the wrong theme.
    applyTheme();
    document.addEventListener('DOMContentLoaded', function () {
        applyTheme();
        document.querySelector('.theme-toggle')?.addEventListener('click', function () {
            preference = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
            try { localStorage.setItem(storageKey, preference); } catch (_) { /* Keep the selection for this page. */ }
            applyTheme();
        });
    });
    systemTheme.addEventListener('change', applyTheme);
    window.addEventListener('storage', function (event) {
        if (event.key !== storageKey && event.key !== null) return;
        preference = event.newValue === 'light' || event.newValue === 'dark' ? event.newValue : null;
        applyTheme();
    });
})();
