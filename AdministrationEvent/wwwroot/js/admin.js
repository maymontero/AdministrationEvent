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
