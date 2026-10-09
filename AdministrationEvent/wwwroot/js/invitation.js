(() => {
  const countdown = document.querySelector('[data-date]');
  if (countdown) {
    const daysOnly = countdown.dataset.daysOnly === 'true';
    const confirmed = !daysOnly && countdown.dataset.confirmed === 'true';
    const target = new Date(countdown.dataset.target).getTime();
    const dateParts = countdown.dataset.date.split('-').map(Number);
    const targetDay = Date.UTC(dateParts[0], dateParts[1] - 1, dateParts[2]);
    const formatter = new Intl.DateTimeFormat('en-CA', { timeZone: countdown.dataset.zone, year: 'numeric', month: '2-digit', day: '2-digit' });
    const tick = () => {
      const now = new Date();
      const parts = Object.fromEntries(formatter.formatToParts(now).map(p => [p.type, p.value]));
      const today = Date.UTC(Number(parts.year), Number(parts.month) - 1, Number(parts.day));
      const remaining = Math.max(0, confirmed ? target - now.getTime() : targetDay - today);
      if (daysOnly && remaining === 0) {
        countdown.hidden = true;
        const section = countdown.closest('.relaxed-countdown');
        section.querySelector('.countdown-intro').textContent = 'Gracias por acompañarnos';
        section.querySelector('[data-countdown-caption]').textContent = 'Gracias por compartir con nosotros este día tan especial.';
        return;
      }
      countdown.querySelector('[data-days]').textContent = Math.floor(remaining / 86400000);
      if (confirmed) {
        countdown.querySelector('[data-hours]').textContent = String(Math.floor(remaining / 3600000) % 24).padStart(2, '0');
        countdown.querySelector('[data-minutes]').textContent = String(Math.floor(remaining / 60000) % 60).padStart(2, '0');
      }
      if (remaining === 0) document.querySelector('[data-countdown-caption]').textContent = today > targetDay ? 'Un día para recordar siempre' : '¡Llegó el día de celebrar!';
    };
    tick(); setInterval(tick, 60000);
  }
  const attendance = document.querySelector('#guest-attending');
  const guestCount = document.querySelector('[data-guest-count]');
  if (attendance && guestCount) {
    const update = () => { const absent = attendance.value === 'false'; guestCount.hidden = absent; guestCount.querySelector('select').disabled = absent; };
    attendance.addEventListener('change', update); update();
  }
})();
