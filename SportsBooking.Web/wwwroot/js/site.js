// Допълнителна проверка в браузъра. Сървърът винаги проверява отново.
document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('form[data-booking-form]');
    if (!form) return;

    const start = form.querySelector('[name="StartTime"]');
    const end = form.querySelector('[name="EndTime"]');
    const date = form.querySelector('[name="BookingDate"]');

    function validate() {
        if (!start || !end || !date) return;
        end.setCustomValidity(end.value && start.value && end.value <= start.value
            ? 'Крайният час трябва да е след началния.' : '');
        const today = new Date();
        const localToday = [today.getFullYear(), String(today.getMonth() + 1).padStart(2, '0'), String(today.getDate()).padStart(2, '0')].join('-');
        date.setCustomValidity(date.value && date.value < localToday
            ? 'Не може да резервирате минала дата.' : '');
    }

    ['input', 'change'].forEach(eventName => form.addEventListener(eventName, validate));
    form.addEventListener('submit', validate);
});

// Safe local demo: the server only renders these buttons when Development demos are enabled.
document.querySelectorAll('.demo-fill').forEach(function (button) {
    button.addEventListener('click', function () {
        const email = document.querySelector('input[name="Email"]');
        const password = document.querySelector('input[name="Password"]');
        if (email && password) {
            email.value = button.dataset.demoEmail || '';
            password.value = button.dataset.demoPassword || '';
            email.dispatchEvent(new Event('input', { bubbles: true }));
            password.dispatchEvent(new Event('input', { bubbles: true }));
            password.focus();
        }
    });
});

// Active navigation item, including controller subroutes.
(function () {
    const current = window.location.pathname.toLowerCase();
    document.querySelectorAll('.site-nav a').forEach(function (link) {
        const href = new URL(link.href).pathname.toLowerCase();
        if ((href === '/' && current === '/') || (href !== '/' && (current === href || current.startsWith(href + '/')))) {
            link.classList.add('active');
            link.setAttribute('aria-current', 'page');
        }
    });
})();

// Accessible mobile navigation; desktop navigation remains visible without JavaScript.
(function () {
    const toggle = document.querySelector('.nav-toggle');
    const nav = document.getElementById('site-navigation');
    if (!toggle || !nav) return;
    function closeNavigation() {
        toggle.setAttribute('aria-expanded', 'false');
        toggle.setAttribute('aria-label', 'Отвори менюто');
        nav.classList.remove('is-open');
    }
    toggle.addEventListener('click', function () {
        const open = toggle.getAttribute('aria-expanded') !== 'true';
        toggle.setAttribute('aria-expanded', String(open));
        toggle.setAttribute('aria-label', open ? 'Затвори менюто' : 'Отвори менюто');
        nav.classList.toggle('is-open', open);
    });
    document.addEventListener('keydown', function (event) {
        if (event.key !== 'Escape') return;
        if (nav.classList.contains('is-open')) { closeNavigation(); toggle.focus(); }
        const account = document.querySelector('.account-menu[open]');
        if (account) { account.open = false; account.querySelector('summary').focus(); }
    });
    document.addEventListener('click', function (event) {
        const account = document.querySelector('.account-menu[open]');
        if (account && !account.contains(event.target)) account.open = false;
        if (!nav.contains(event.target) && !toggle.contains(event.target)) closeNavigation();
    });
})();
