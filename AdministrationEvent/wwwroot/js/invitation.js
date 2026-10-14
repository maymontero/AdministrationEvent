(() => {
  const spotify = document.querySelector('[data-spotify-music]');
  if (spotify) {
    const button = spotify.querySelector('[data-spotify-toggle]');
    const status = spotify.querySelector('[data-spotify-status]');
    let controller = null, ready = false, paused = true, pending = null;
    const clearPending = () => { if (pending) clearTimeout(pending); pending = null; };
    const fallback = () => {
      clearPending(); button.disabled = !ready;
      if (!ready) button.textContent = '♫ Spotify no disponible';
      status.textContent = 'Tocá Play en Spotify para escuchar.';
    };
    const update = isPaused => {
      paused = isPaused; clearPending(); button.disabled = false;
      button.textContent = paused ? '♫ Reproducir' : '♫ Pausar';
      button.setAttribute('aria-pressed', String(!paused));
      status.textContent = paused ? 'Reproducción pausada' : 'Sonando en Spotify';
    };
    button.hidden = false;
    button.disabled = true;
    button.textContent = '♫ Cargando…';
    const eagerLoad = () => {
      spotify.querySelectorAll('iframe').forEach(frame => { frame.loading = 'eager'; });
    };
    // The API replaces the original embed; load its replacement even below the fold.
    const observer = new MutationObserver(eagerLoad);
    observer.observe(spotify, { childList: true, subtree: true });
    eagerLoad();
    pending = setTimeout(fallback, 20000);
    button.addEventListener('click', () => {
      if (!ready) { fallback(); return; }
      button.disabled = true;
      pending = setTimeout(fallback, 5000);
      try { if (paused) controller.resume(); else controller.pause(); } catch { fallback(); }
    });
    window.onSpotifyIframeApiReady = api => {
      try {
        api.createController(spotify.querySelector('[data-spotify-embed]'), {
          uri: 'spotify:track:' + spotify.dataset.track, width: '100%', height: 152, theme: 'dark'
        }, embed => {
          controller = embed;
          eagerLoad();
          embed.addListener('ready', () => { ready = true; observer.disconnect(); update(paused); });
          embed.addListener('playback_started', () => { update(false); });
          embed.addListener('playback_update', event => {
            if (typeof event.data?.isPaused === 'boolean' && !event.data.isBuffering) update(event.data.isPaused);
          });
        });
      } catch { fallback(); }
    };
    const script = document.createElement('script');
    script.src = 'https://open.spotify.com/embed/iframe-api/v1';
    script.async = true;
    script.addEventListener('error', fallback);
    document.head.appendChild(script);
  }
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
        const section = countdown.closest('.relaxed-countdown, .countdown-band');
        const intro = section.querySelector('.countdown-intro, .eyebrow');
        if (intro) intro.textContent = 'Gracias por acompañarnos';
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
