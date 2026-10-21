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
