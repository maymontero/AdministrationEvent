(() => {
  const root = document.querySelector('#seating-editor'); if (!root) return;
  const data = JSON.parse(document.querySelector('#plan-data').textContent);
  const board = root.querySelector('.plan-board'), layer = root.querySelector('#plan-tables');
  const status = root.querySelector('#plan-status'), existing = root.querySelector('#existing-table');
  const tables = new Map(data.tables.map(t => [t.id, t]));
  let positions = data.positions.filter(p => tables.has(p.TableId)), selected = null, venue = data.useVenueImage, hasImage = root.dataset.hasImage === 'true', dirty = false, revision = 0;
  const tooltip = document.createElement('div'); tooltip.id = 'table-tooltip'; tooltip.className = 'plan-tooltip'; tooltip.setAttribute('role', 'tooltip'); tooltip.hidden = true; board.append(tooltip);
  const describe = p => { tooltip.textContent = `${tables.get(p.TableId).name}: ${p.Description || ''}`; tooltip.hidden = !p.Description; tooltip.style.left = Math.max(25, Math.min(75, p.X)) + '%'; tooltip.style.top = (p.Y > 70 ? p.Y - 20 : p.Y + 10) + '%'; };
  const saveButton = root.querySelector('#save-plan');
  const toast = document.createElement('div'); toast.className = 'plan-save-notice'; toast.hidden = true; toast.setAttribute('role', 'status'); toast.setAttribute('aria-live', 'polite'); toast.setAttribute('aria-atomic', 'true');
  const toastText = document.createElement('span');
  const closeNotice = document.createElement('button'); closeNotice.type = 'button'; closeNotice.textContent = '×'; closeNotice.setAttribute('aria-label', 'Cerrar aviso');
  toast.append(toastText, closeNotice); root.append(toast);
  let noticeTimer;
  const hideNotice = () => { toast.hidden = true; clearTimeout(noticeTimer); };
  closeNotice.addEventListener('click', hideNotice);
  const notify = (text, kind = 'success') => { clearTimeout(noticeTimer); toast.className = 'plan-save-notice ' + kind; toastText.textContent = text; toast.hidden = false; noticeTimer = setTimeout(hideNotice, kind === 'success' ? 6500 : 12000); };
  const token = root.querySelector('[name="__RequestVerificationToken"]').value;
  const message = (text, error = false) => { status.textContent = text; status.classList.toggle('error', error); };
  const changed = () => { revision++; dirty = true; saveButton.textContent = 'Guardar distribución'; saveButton.classList.remove('plan-saved'); hideNotice(); message('Hay cambios sin guardar.'); };
  async function request(path, body, json = false) {
    const response = await fetch(root.dataset.url + path, { method: 'POST', headers: { RequestVerificationToken: token, ...(json ? {'Content-Type':'application/json'} : {}) }, body: json ? JSON.stringify(body) : body });
    if (response.redirected) throw new Error('La sesión terminó. Volvé a iniciar sesión para guardar.');
    if (!response.ok) { let text = 'No se pudo guardar. Intentá de nuevo.'; try { text = (await response.json()).error || text; } catch {} throw new Error(text); }
    return response;
  }
  const clamp = (v, min, max) => Math.min(max, Math.max(min, v));
  function mode(value) {
    venue = value;
    root.querySelectorAll('[data-mode]').forEach(b => b.setAttribute('aria-pressed', String((b.dataset.mode === 'venue') === venue)));
    root.querySelector('.plan-upload').hidden = !venue;
    board.classList.toggle('venue', venue);
    const image = root.querySelector('#venue-image'); image.hidden = !venue || !hasImage;
    if (!image.hidden && !image.getAttribute('src')) image.src = root.dataset.url + '/image';
  }
  function select(id) {
    selected = id; const p = positions.find(p => p.TableId === id), t = tables.get(id);
    root.querySelector('#selected-table').hidden = !p;
    if (p) { root.querySelector('#selected-name').textContent = t.name; root.querySelector('#selected-capacity').textContent = `${t.capacity} lugares`; root.querySelector('#table-shape').value = p.Shape; root.querySelector('#table-description').value = p.Description || ''; }
    layer.querySelectorAll('button').forEach(b => b.setAttribute('aria-pressed', String(Number(b.dataset.id) === id)));
  }
  function locate(button, p) { button.style.left = p.X + '%'; button.style.top = p.Y + '%'; button.style.transform = `translate(-50%,-50%) rotate(${p.Rotation}deg)`; }
  function render() {
    tooltip.hidden = true;
    layer.replaceChildren();
    positions.forEach(p => {
      const t = tables.get(p.TableId), button = document.createElement('button'); button.type = 'button'; button.className = 'plan-table ' + p.Shape; button.dataset.id = p.TableId;
      button.textContent = t.name; button.setAttribute('aria-label', `${t.name}, ${t.capacity} lugares. Usá las flechas para mover.`); locate(button,p);
      button.setAttribute('aria-describedby', tooltip.id);
      button.addEventListener('mouseenter', () => describe(p));
      button.addEventListener('mouseleave', () => { tooltip.hidden = true; });
      button.addEventListener('focus', () => describe(p));
      button.addEventListener('blur', () => { tooltip.hidden = true; });
      button.addEventListener('click', () => select(p.TableId));
      button.addEventListener('keydown', e => { const moves = {ArrowLeft:[-1,0],ArrowRight:[1,0],ArrowUp:[0,-1],ArrowDown:[0,1]}; if (!moves[e.key]) return; e.preventDefault(); select(p.TableId); const [x,y] = moves[e.key]; p.X=clamp(p.X+x,6,94); p.Y=clamp(p.Y+y,7,93); locate(button,p); changed(); });
      let drag = null;
      button.addEventListener('pointerdown', e => { if(e.button !== 0) return; tooltip.hidden = true; select(p.TableId); const rect = board.getBoundingClientRect(); drag = {x:e.clientX,y:e.clientY,px:p.X,py:p.Y,rect}; button.setPointerCapture(e.pointerId); });
      button.addEventListener('pointermove', e => { if(!drag) return; p.X=clamp(drag.px+(e.clientX-drag.x)/drag.rect.width*100,6,94); p.Y=clamp(drag.py+(e.clientY-drag.y)/drag.rect.height*100,7,93); locate(button,p); });
      const finish = () => { if(!drag)return; if(p.X!==drag.px||p.Y!==drag.py)changed(); drag=null; }; button.addEventListener('pointerup',finish); button.addEventListener('pointercancel',finish);
      layer.append(button);
    });
    existing.replaceChildren(new Option('Elegí una mesa',''));
    tables.forEach(t => { if(!positions.some(p=>p.TableId===t.id))existing.add(new Option(`${t.name} · ${t.capacity} lugares`,t.id)); });
    root.querySelector('#placed-count').textContent = `${positions.length} mesas en el plano`;
    root.querySelector('.plan-empty').hidden = positions.length > 0 || (venue && hasImage);
    select(selected);
  }
  function place(id) { if(positions.length>=200){message('El plano admite hasta 200 mesas.',true);return;} positions.push({TableId:id,X:25+(positions.length%4)*16,Y:25+(Math.floor(positions.length/4)%4)*16,Shape:'round',Rotation:0}); selected=id;render();changed(); }
  root.querySelectorAll('[data-mode]').forEach(b=>b.addEventListener('click',()=>{mode(b.dataset.mode==='venue');render();changed();}));
  root.querySelector('#place-table').addEventListener('click',()=>{const id=Number(existing.value);if(id)place(id);});
  root.querySelector('#add-plan-table').addEventListener('submit',async e=>{e.preventDefault();const button=e.submitter;button.disabled=true;try{const body=new FormData();body.set('name',root.querySelector('#plan-name').value);body.set('capacity',root.querySelector('#plan-capacity').value);const t=await(await request('/table',body)).json();tables.set(t.id,t);place(t.id);root.querySelector('#plan-name').value='';}catch(e){message(e.message,true);}finally{button.disabled=false;}});
  root.querySelector('#table-description').addEventListener('input', e => { const p = positions.find(p => p.TableId === selected); if (p) { p.Description = e.target.value; changed(); } });
  root.querySelector('#table-shape').addEventListener('change',e=>{const p=positions.find(p=>p.TableId===selected);if(p){p.Shape=e.target.value;render();changed();}});
  root.querySelector('#rotate-table').addEventListener('click',()=>{const p=positions.find(p=>p.TableId===selected);if(p){p.Rotation=(p.Rotation+45)%360;render();changed();}});
  root.querySelector('#unplace-table').addEventListener('click',()=>{positions=positions.filter(p=>p.TableId!==selected);selected=null;render();changed();});
  saveButton.addEventListener('click', async () => {
    saveButton.disabled = true; saveButton.textContent = 'Guardando…'; saveButton.classList.remove('plan-saved'); hideNotice();
    const savingRevision = revision;
    try {
      await request('', {tables:positions,useVenueImage:venue}, true);
      if (revision === savingRevision) {
        dirty = false; message('Distribución guardada.'); saveButton.textContent = '✓ Guardado'; saveButton.classList.add('plan-saved'); notify('✓ Distribución guardada correctamente');
      } else {
        saveButton.textContent = 'Guardar distribución'; message('Se guardó la distribución anterior. Guardá los últimos cambios.'); notify('Se guardó la versión anterior. Tenés cambios nuevos por guardar.', 'warning');
      }
    } catch (error) {
      saveButton.textContent = 'Guardar distribución'; const text = error instanceof TypeError ? 'No se pudo guardar. Revisá la conexión e intentá de nuevo.' : error.message; message(text, true); notify(text, 'error');
    } finally { saveButton.disabled = false; }
  });
  root.querySelector('#venue-file').addEventListener('change',async e=>{const file=e.target.files[0];if(!file)return;if(file.size>5*1024*1024){message('La imagen debe pesar hasta 5 MB.',true);return;}const body=new FormData();body.set('file',file);e.target.disabled=true;try{await request('/image',body);hasImage=true;root.querySelector('#venue-image').src=root.dataset.url+'/image?v='+Date.now();mode(true);render();changed();message('Plano subido. Acomodá las mesas y guardá la distribución.');}catch(error){message(error.message,true);}finally{e.target.disabled=false;}});
  window.addEventListener('beforeunload',e=>{if(dirty){e.preventDefault();e.returnValue='';}});
  mode(venue);render();
})();
