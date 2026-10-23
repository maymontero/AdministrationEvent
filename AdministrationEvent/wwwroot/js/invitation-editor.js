(() => {
  const form = document.querySelector('[data-invitation-editor]');
  if (!form) return;
  const source = form.querySelector('textarea[name="ConfigurationJson"]');
  const advanced = form.querySelector('[data-advanced-json]');
  const list = form.querySelector('[data-sections]');
  const template = document.querySelector('#section-template');
  const layoutStatus = form.querySelector('[data-layout-status]');
  const sharingLink = form.querySelector('[data-sharing-link]');
  const assetDialog = document.querySelector('[data-asset-dialog]');
  const assetContent = assetDialog?.querySelector('[data-asset-content]');
  let assets = null;
  let activeAssetInput = null;
  let config;
  try { config = JSON.parse(source.value); } catch { return; }
  const normalize = () => {
    config.Theme ||= {};
    config.Sections ||= [];
    config.Sections.forEach(section => { section.DisplayMode ||= 'standard'; section.Enabled ??= true; section.LinkLabel ??= 'Ver más'; });
  };
  normalize();

  const get = (object, path) => path.split('.').reduce((value, key) => value?.[key], object);
  const set = (object, path, value) => {
    const parts = path.split('.'); const key = parts.pop();
    const target = parts.reduce((item, part) => item[part] ||= {}, object); target[key] = value;
  };
  const localDate = value => value ? new Date(value).toISOString().slice(0, 16) : '';
  const bindFields = () => form.querySelectorAll('[data-field]').forEach(input => {
    const value = get(config, input.dataset.field);
    if (input.type === 'checkbox') input.checked = Boolean(value);
    else input.value = input.type === 'datetime-local' ? localDate(value) : value ?? '';
    input.addEventListener('input', () => set(config, input.dataset.field, input.type === 'checkbox' ? input.checked : (input.type === 'datetime-local' ? (input.value || null) : input.value)));
    input.addEventListener('change', () => set(config, input.dataset.field, input.type === 'checkbox' ? input.checked : (input.type === 'datetime-local' ? (input.value || null) : input.value)));
  });
  const renderLayoutStatus = () => {
    if (config.Theme.Layout === 'custom') { layoutStatus.innerHTML = '<strong>Vista libre</strong><p>Esta invitación se arma solamente con tus bloques, colores, fuentes e imágenes.</p>'; return; }
    layoutStatus.innerHTML = `<strong>Diseño histórico preservado</strong><p>Esta tarjeta conserva su composición original para no cambiar lo ya aprobado.</p><button type="button" class="quiet-button" data-switch-custom>Pasarla a vista libre</button>`;
    layoutStatus.querySelector('[data-switch-custom]').addEventListener('click', () => { config.Theme.Layout = 'custom'; renderLayoutStatus(); });
  };
  const renderSharingLink = () => {
    if (!sharingLink) return;
    if (!config.IsPublished) { sharingLink.innerHTML = '<p>Al publicarla, se habilita el enlace que elijas compartir.</p>'; return; }
    if (config.SharingMode === 'link' && !config.ShareKey) { sharingLink.innerHTML = '<p>Al guardar se generará un enlace privado único. Después compartilo solo con las personas elegidas.</p>'; return; }
    const path = config.SharingMode === 'link' ? `/${config.Slug}/acceso/${config.ShareKey}` : `/${config.Slug}`;
    const url = `${window.location.origin}${path}`;
    sharingLink.innerHTML = `<strong>${config.SharingMode === 'link' ? 'Enlace privado' : 'Enlace público'}</strong><code>${escape(url)}</code><button type="button" class="quiet-button" data-copy-link>Copiar enlace</button>`;
    sharingLink.querySelector('[data-copy-link]').addEventListener('click', async () => { await navigator.clipboard.writeText(url); sharingLink.querySelector('[data-copy-link]').textContent = 'Copiado'; });
  };
  const normalizeColor = value => {
    const hex = value.trim().replace(/^#/, '');
    if (!/^(?:[a-f0-9]{3}|[a-f0-9]{6})$/i.test(hex)) return null;
    return '#' + (hex.length === 3 ? [...hex].map(char => char + char).join('') : hex).toUpperCase();
  };
  const bindColors = () => form.querySelectorAll('[data-color]').forEach(input => {
    const hex = input.closest('[data-color-field]').querySelector('[data-color-hex]');
    const apply = value => { input.value = value; input.style.backgroundColor = value; set(config, input.dataset.color, value); };
    apply(normalizeColor(get(config, input.dataset.color) || '#ffffff') || '#FFFFFF');
    hex.value = input.value.toUpperCase();
    const clearError = () => { hex.setCustomValidity(''); hex.removeAttribute('aria-invalid'); };
    const pick = () => { const value = normalizeColor(input.value); apply(value); hex.value = value; clearError(); };
    input.oninput = pick; input.onchange = pick;
    const type = commit => {
      const value = normalizeColor(hex.value);
      if (!value) { hex.setCustomValidity('Escribí un código de 3 o 6 caracteres, por ejemplo #C66FFF.'); hex.setAttribute('aria-invalid', 'true'); return; }
      clearError(); apply(value); if (commit) hex.value = value;
    };
    hex.oninput = () => type(false);
    hex.onchange = () => type(true);
    hex.onblur = () => type(true);
  });

  bindColors();

  const newSection = () => ({ DisplayMode: 'standard', Enabled: true, Title: 'Nueva sección', Content: '', ImageUrl: '', LinkLabel: 'Ver más', LinkUrl: '' });
  const addSection = (section) => {
    const fragment = template.content.cloneNode(true); const card = fragment.querySelector('.section-editor');
    card.querySelectorAll('[data-section]').forEach(input => {
      const key = input.dataset.section; const value = section[key];
      if (input.type === 'checkbox') input.checked = value !== false;
      else input.value = value ?? '';
      const save = () => { section[key] = input.type === 'checkbox' ? input.checked : (key === 'TitleFontSize' ? (input.value ? Number(input.value) : null) : input.value); card.querySelector('[data-section-name]').textContent = section.Title || 'Sin título'; };
      input.addEventListener('input', save); input.addEventListener('change', save);
    });
    card.querySelector('[data-section-name]').textContent = section.Title || 'Sin título';
    card.querySelector('[data-remove-section]').addEventListener('click', () => { config.Sections.splice([...list.children].indexOf(card), 1); card.remove(); });
    card.addEventListener('dragstart', event => { card.classList.add('dragging'); event.dataTransfer.effectAllowed = 'move'; });
    card.addEventListener('dragend', () => { card.classList.remove('dragging'); syncOrder(); });
    list.appendChild(fragment);
  };
  const syncOrder = () => { config.Sections = [...list.children].map(card => card.__section); };
  const originalAdd = addSection;
  // Associate each visible card with its plain configuration object after insertion.
  const renderSections = () => { list.innerHTML = ''; config.Sections.forEach(section => { originalAdd(section); list.lastElementChild.__section = section; }); };
  list.addEventListener('dragover', event => {
    event.preventDefault(); const moving = list.querySelector('.dragging'); if (!moving) return;
    const after = [...list.querySelectorAll('.section-editor:not(.dragging)')].find(card => event.clientY < card.getBoundingClientRect().top + card.offsetHeight / 2);
    list.insertBefore(moving, after || null);
  });
  form.querySelector('[data-add-section]').addEventListener('click', () => { const section = newSection(); config.Sections.push(section); originalAdd(section); list.lastElementChild.__section = section; list.lastElementChild.querySelector('[data-section="Title"]').focus(); });
  advanced.addEventListener('change', () => {
    try { config = JSON.parse(advanced.value); normalize(); bindFields(); bindColors(); renderSections(); renderLayoutStatus(); renderSharingLink(); advanced.setCustomValidity(''); }
    catch { advanced.setCustomValidity('El JSON no es válido.'); }
  });
  form.addEventListener('submit', () => { syncOrder(); source.value = JSON.stringify(config); advanced.value = JSON.stringify(config, null, 2); });
  const escape = value => String(value).replace(/[&<>"]/g, c => ({ '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;' }[c]));
  const loadAssets = async () => {
    if (assets) return assets;
    const query = new URLSearchParams({ invitationId: config.Id });
    const response = await fetch(`/admin/assets?${query}`, { headers: { Accept: 'application/json' } });
    if (!response.ok) throw new Error(`La biblioteca devolvió ${response.status}.`);
    assets = await response.json();
    return assets;
  };
  const showAssetError = () => window.alert('No pudimos cargar la biblioteca de imágenes y tipografías. Verificá que las migraciones de la aplicación estén aplicadas y volvé a intentar.');
  const setAsset = (url, fontTarget) => {
    if (fontTarget) {
      const family = `InvitationAsset${fontTarget === 'heading' ? 'Heading' : 'Body'}`;
      const style = document.createElement('style'); style.textContent = `@font-face{font-family:${family};src:url('${url}');font-display:swap}`; document.head.appendChild(style);
      set(config, fontTarget === 'heading' ? 'Theme.HeadingFont' : 'Theme.BodyFont', family);
      set(config, fontTarget === 'heading' ? 'Theme.HeadingFontUrl' : 'Theme.BodyFontUrl', url);
      form.querySelector(`[data-field="Theme.${fontTarget === 'heading' ? 'HeadingFont' : 'BodyFont'}"]`).value = family;
    } else if (activeAssetInput) { activeAssetInput.value = url; activeAssetInput.dispatchEvent(new Event('input', { bubbles: true })); }
    assetDialog.close();
  };
  const openImages = async input => {
    try {
      activeAssetInput = input; const library = await loadAssets();
      assetDialog.dataset.assetKind = 'image';
      assetDialog.querySelector('[data-asset-title]').textContent = 'Elegir una imagen';
      assetContent.innerHTML = `<div class="asset-grid">${library.images.map(url => `<button type="button" data-asset-url="${escape(url)}"><img src="${escape(url)}" alt=""><span>${escape(url.split('/').pop())}</span></button>`).join('')}</div>`;
      assetContent.querySelectorAll('[data-asset-url]').forEach(button => button.addEventListener('click', () => setAsset(button.dataset.assetUrl)));
      assetDialog.showModal();
    } catch { showAssetError(); }
  };
  const openFonts = async target => {
    try {
      const library = await loadAssets();
      assetDialog.dataset.assetKind = 'font';
      assetDialog.querySelector('[data-asset-title]').textContent = 'Elegir una tipografía';
      assetContent.innerHTML = `<div class="font-grid">${library.fonts.map((font, index) => `<button type="button" data-font-url="${escape(font.url)}" data-font-name="${escape(font.name)}"><style>@font-face{font-family:AssetPreview${index};src:url('${escape(font.url)}')}</style><span style="font-family:AssetPreview${index},serif">Nos casamos</span><small>${escape(font.name)}</small></button>`).join('')}</div>`;
      assetContent.querySelectorAll('[data-font-url]').forEach(button => button.addEventListener('click', () => setAsset(button.dataset.fontUrl, target)));
      assetDialog.showModal();
    } catch { showAssetError(); }
  };
  form.addEventListener('click', event => {
    const image = event.target.closest('[data-image-target]'); if (image) { openImages(image.closest('label').querySelector('input')); return; }
    const font = event.target.closest('[data-font-target]'); if (font) { openFonts(font.dataset.fontTarget); }
  });
  assetDialog?.querySelector('[data-close-assets]').addEventListener('click', () => assetDialog.close());
  assetDialog?.querySelector('[data-upload-form]').addEventListener('submit', async event => {
    event.preventDefault(); const file = assetDialog.querySelector('[data-upload-file]').files[0]; if (!file) return;
    const body = new FormData(); body.append('file', file); body.append('invitationId', config.Id); body.append('__RequestVerificationToken', form.querySelector('input[name="__RequestVerificationToken"]').value);
    const response = await fetch('/admin/assets/upload', { method: 'POST', body }); const result = await response.json();
    if (!response.ok) { window.alert(result.error || 'No se pudo subir la imagen.'); return; } setAsset(result.url);
  });
  bindFields();
  form.querySelector('[data-field="IsPublished"]')?.addEventListener('change', renderSharingLink);
  form.querySelector('[data-field="SharingMode"]')?.addEventListener('change', renderSharingLink);
  form.querySelector('[data-field="Slug"]')?.addEventListener('input', renderSharingLink);
  renderSections(); renderLayoutStatus(); renderSharingLink(); advanced.value = JSON.stringify(config, null, 2);
})();
