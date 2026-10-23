(() => {
  document.querySelectorAll('input[type="password"]').forEach(input => {
    const wrapper = document.createElement('div');
    wrapper.className = 'password-field';
    input.before(wrapper);
    wrapper.appendChild(input);
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'password-visibility';
    button.setAttribute('aria-controls', input.id);
    button.setAttribute('aria-pressed', 'false');
    button.setAttribute('aria-label', 'Mostrar contraseña');
    button.title = 'Mostrar contraseña';
    button.innerHTML = '<svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z"/><circle cx="12" cy="12" r="3"/><path data-eye-slash d="M4 4l16 16" hidden /></svg>';
    const slash = button.querySelector('[data-eye-slash]');
    const setVisible = visible => {
      input.type = visible ? 'text' : 'password';
      button.setAttribute('aria-pressed', String(visible));
      const label = visible ? 'Ocultar contraseña' : 'Mostrar contraseña';
      button.setAttribute('aria-label', label);
      button.title = label;
      slash.style.display = visible ? '' : 'none';
      slash.removeAttribute('hidden');
    };
    setVisible(false);
    button.addEventListener('click', () => setVisible(input.type === 'password'));
    input.form?.addEventListener('submit', () => setVisible(false));
    wrapper.appendChild(button);
  });
})();

// Avoid duplicate requests while a save is in progress, without losing edited JSON.
document.querySelectorAll('form:has([data-save-once]),form[action*="/save-date/"]').forEach(form => {
  form.addEventListener('submit', event => {
    if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
    form.dataset.submitting = 'true';
    form.querySelectorAll('button[type="submit"],button:not([type])').forEach(button => { button.disabled = true; });
  });
});
window.addEventListener('pageshow', () => {
  document.querySelectorAll('form[data-submitting]').forEach(form => {
    delete form.dataset.submitting;
    form.querySelectorAll('button').forEach(button => { button.disabled = false; });
  });
});

// Native disclosures keep row actions accessible with touch, mouse and keyboard.
(() => {
  const menus = Array.from(document.querySelectorAll('.invitation-actions-menu'));
  if (!menus.length) return;
  const close = except => menus.forEach(menu => { if (menu !== except) menu.open = false; });
  menus.forEach(menu => {
    const trigger = menu.querySelector('summary'), panel = menu.querySelector('.invitation-row-menu');
    const position = () => {
      if (!menu.open) return;
      panel.classList.add('is-positioned');
      const bounds = trigger.getBoundingClientRect(), box = panel.getBoundingClientRect();
      panel.style.left = Math.max(16, Math.min(window.innerWidth - box.width - 16, bounds.right - box.width)) + 'px';
      const below = bounds.bottom + 5;
      const above = bounds.top - box.height - 5;
      panel.style.top = Math.max(16, Math.min(window.innerHeight - box.height - 16, below + box.height <= window.innerHeight - 16 ? below : above)) + 'px';
    };
    menu.addEventListener('toggle', () => { if (menu.open) { close(menu); position(); } });
    window.addEventListener('resize', position);
    document.addEventListener('scroll', position, true);
    menu.addEventListener('keydown', event => {
      if (event.key === 'Escape' && menu.open) { event.preventDefault(); menu.open = false; trigger.focus(); }
    });
    // Tab can leave the disclosure; pointer clicks must reach their link before it disappears.
    menu.addEventListener('focusout', event => {
      if (event.relatedTarget && !menu.contains(event.relatedTarget)) menu.open = false;
    });
  });
  document.addEventListener('click', event => { if (!event.target.closest('.invitation-actions-menu')) close(); });

})();
