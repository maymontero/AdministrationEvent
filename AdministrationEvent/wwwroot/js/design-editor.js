(() => {
 const root=document.querySelector('#design-editor');if(!root)return;
 let doc=JSON.parse(document.querySelector('#design-data').textContent),version=Number(root.dataset.version),sectionIndex=0,elementIndex=-1,dirty=false,revision=0;
 const stage=root.querySelector('#design-stage'),status=root.querySelector('#design-status'),token=root.querySelector('[name="__RequestVerificationToken"]').value;
 let undo=[],redo=[],assets=[];
 const baseFonts=['Georgia, serif','Arial, sans-serif','Verdana, sans-serif','Palatino, serif','Courier New, monospace'];
 const colorControls=[];
 let fonts=baseFonts.map(name=>({name,family:name,url:''}));
 const fontObserver=new IntersectionObserver(entries=>entries.forEach(entry=>{if(!entry.isIntersecting)return;const url=entry.target.dataset.fontUrl;if(url)InvitationCanvas.fontFamily(url);fontObserver.unobserve(entry.target);}),{root:root.querySelector('#font-samples')});
 const $=id=>root.querySelector('#'+id),section=()=>doc.sections[sectionIndex],element=()=>section().elements[elementIndex];
 const msg=(s,error=false)=>{status.textContent=s;status.classList.toggle('error',error);};
 const clamp=(n,a,b)=>Math.min(b,Math.max(a,n));
 function remember(){undo.push(JSON.stringify(doc));if(undo.length>60)undo.shift();redo=[];}
 function change(){$('design-undo').disabled=!undo.length;$('design-redo').disabled=!redo.length;dirty=true;revision++;msg('Cambios sin guardar.');}
 function fit(e){e.width=clamp(e.width,3,100);e.height=clamp(e.height,20,section().height);e.x=clamp(e.x,0,100-e.width);e.y=clamp(e.y,0,section().height-e.height);}
 function mutate(fn){remember();fn();change();render();}
 function renderSections(){const list=$('design-sections');list.replaceChildren();doc.sections.forEach((s,i)=>{const b=document.createElement('button');b.type='button';b.textContent=`${i+1}. ${s.name}`;b.setAttribute('aria-pressed',String(i===sectionIndex));b.onclick=()=>{sectionIndex=i;elementIndex=-1;render();};list.append(b);});}
 function inspector(){
  const s=section(),e=element();$('section-name').value=s.name;$('section-height').value=s.height;$('section-bg').value=s.background;$('section-image').value=s.backgroundImage;
  colorControls.forEach(control=>control.sync());
  $('element-properties').hidden=!e;$('no-element').hidden=!!e;
  if(!e)return;
  $('element-section').replaceChildren();doc.sections.forEach((s,i)=>$('element-section').add(new Option(`${i+1}. ${s.name}`,i)));$('element-section').value=sectionIndex;
  for(const [id,key]of Object.entries({'element-text':'text','element-image':'image','element-link':'link','element-font':'font','element-size':'fontSize','element-color':'color','element-bg':'background','element-align':'align','element-x':'x','element-y':'y','element-width':'width','element-height':'height'}))$(id).value=e[key]??'';
  updateCurrentFont();
 }
 function updateCurrentFont(){
  const e=element();if(!e)return;
  const font=fonts.find(f=>e.fontUrl?f.url===e.fontUrl:!f.url&&f.family===e.font);
  $('font-current-name').textContent=font?.name || e.fontUrl?.split('/').pop() || e.font;
  $('font-current-sample').style.fontFamily=e.fontUrl?InvitationCanvas.fontFamily(e.fontUrl):e.font;
  $('font-samples').querySelectorAll('button').forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.fontKey===(e.fontUrl||e.font))));
 }
 function renderFonts(){
  fontObserver.disconnect();const list=$('font-samples');list.replaceChildren();
  const query=$('font-search').value.trim().toLocaleLowerCase();const filtered=fonts.filter(f=>f.name.toLocaleLowerCase().includes(query));
  filtered.forEach(font=>{
   const button=document.createElement('button');button.type='button';button.className='font-sample-option';button.dataset.fontKey=font.url||font.family;button.setAttribute('aria-label',font.name);
   const name=document.createElement('span');name.className='font-sample-name';name.textContent=font.name;
   const sample=document.createElement('span');sample.className='font-sample-preview';sample.textContent='Amor · Aa';sample.setAttribute('aria-hidden','true');sample.style.fontFamily=font.family;
   if(font.url)sample.dataset.fontUrl=font.url;
   button.append(name,sample);button.onclick=()=>{if(!element())return;mutate(()=>{element().font=font.family;element().fontUrl=font.url;});$('font-picker-panel').hidden=true;$('font-picker-toggle').setAttribute('aria-expanded','false');$('font-picker-toggle').focus();};
   list.append(button);if(font.url)fontObserver.observe(sample);
  });
  $('font-results').textContent=filtered.length?`${filtered.length} tipografías`:'No encontramos esa tipografía.';updateCurrentFont();
 }
 $('font-picker-toggle').onclick=()=>{const panel=$('font-picker-panel');panel.hidden=!panel.hidden;$('font-picker-toggle').setAttribute('aria-expanded',String(!panel.hidden));if(!panel.hidden)renderFonts();};
 $('font-search').addEventListener('input',renderFonts);
 $('font-picker-panel').addEventListener('keydown',event=>{if(event.key==='Escape'){$('font-picker-panel').hidden=true;$('font-picker-toggle').setAttribute('aria-expanded','false');$('font-picker-toggle').focus();}});
 function loadFontLibrary(library){
  const seen=new Set();fonts=baseFonts.map(name=>({name,family:name,url:''}));
  library.forEach(font=>{if(seen.has(font.name.toLowerCase()))return;seen.add(font.name.toLowerCase());fonts.push({name:font.name,url:font.url,family:InvitationCanvas.fontFamily(font.url,false)});});
  $('element-font').replaceChildren();fonts.forEach(font=>$('element-font').add(new Option(font.name,font.family)));
  updateCurrentFont();if(!$('font-picker-panel').hidden)renderFonts();
 }
 function renderStage(){
  stage.replaceChildren();const inner=document.createElement('div');inner.className='design-stage-inner';
  const label=document.createElement('div');label.className='design-section-label';label.textContent=section().name;inner.append(label);
  const board=InvitationCanvas.section({...section(),elements:[]},root.dataset.date,true);board.onclick=e=>{if(e.target===board){elementIndex=-1;renderStage();inspector();}};
  section().elements.forEach((e,i)=>{
   const node=InvitationCanvas.element(e,root.dataset.date,true);node.tabIndex=0;node.setAttribute('aria-label',e.type==='image'?'Imagen':e.text||e.type);node.dataset.index=i;
   if(i===elementIndex){node.classList.add('selected');const handle=document.createElement('span');handle.className='design-resize';handle.setAttribute('aria-hidden','true');node.append(handle);}
   node.addEventListener('pointerdown',event=>{
    if(event.button!==0)return;event.preventDefault();event.stopPropagation();
    const resizing=event.target.classList.contains('design-resize');elementIndex=i;inspector();
    board.querySelectorAll('.canvas-element').forEach(n=>{n.classList.toggle('selected',n===node);n.querySelector('.design-resize')?.remove();});
    const handle=document.createElement('span');handle.className='design-resize';node.append(handle);
    const start={x:event.clientX,y:event.clientY,ex:e.x,ey:e.y,w:e.width,h:e.height},bounds=board.getBoundingClientRect();let moved=false;
    node.setPointerCapture(event.pointerId);
    const move=ev=>{const dx=(ev.clientX-start.x)/bounds.width*100,dy=(ev.clientY-start.y)/(bounds.width/900);if(!moved&&(Math.abs(dx)>.2||Math.abs(dy)>2)){remember();moved=true;}if(!moved)return;
     if(resizing){e.width=clamp(start.w+dx,3,100-e.x);e.height=clamp(start.h+dy,20,section().height-e.y);}else{e.x=clamp(start.ex+dx,0,100-e.width);e.y=clamp(start.ey+dy,0,section().height-e.height);}
     Object.assign(node.style,{left:e.x+'%',top:e.y+'px',width:e.width+'%',height:e.height+'px'});
    };
    const finish=()=>{node.removeEventListener('pointermove',move);node.removeEventListener('pointerup',finish);node.removeEventListener('pointercancel',finish);if(moved){e.x=Math.round(e.x*10)/10;e.y=Math.round(e.y);e.width=Math.round(e.width*10)/10;e.height=Math.round(e.height);fit(e);change();}inspector();};
    node.addEventListener('pointermove',move);node.addEventListener('pointerup',finish);node.addEventListener('pointercancel',finish);
   });
   node.addEventListener('keydown',event=>{if(!['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(event.key))return;event.preventDefault();elementIndex=i;remember();const d=event.shiftKey?10:1;if(event.key==='ArrowLeft')e.x-=d/9;if(event.key==='ArrowRight')e.x+=d/9;if(event.key==='ArrowUp')e.y-=d;if(event.key==='ArrowDown')e.y+=d;fit(e);change();renderStage();inspector();stage.querySelector(`[data-index="${i}"]`)?.focus();});board.append(node);
  });inner.append(board);stage.append(inner);scale();
 }
 function scale(){const inner=stage.querySelector('.design-stage-inner');if(!inner)return;const factor=stage.clientWidth/900;stage.style.setProperty('--canvas-scale',factor);inner.style.transform=`scale(${factor})`;stage.style.height=(section().height+26)*factor+'px';}
 function render(){renderSections();renderStage();inspector();$('design-undo').disabled=!undo.length;$('design-redo').disabled=!redo.length;}
 new ResizeObserver(scale).observe(stage);
 $('design-undo').onclick=()=>{if(!undo.length)return;redo.push(JSON.stringify(doc));doc=JSON.parse(undo.pop());sectionIndex=Math.min(sectionIndex,doc.sections.length-1);elementIndex=-1;change();render();};
 $('design-redo').onclick=()=>{if(!redo.length)return;undo.push(JSON.stringify(doc));doc=JSON.parse(redo.pop());sectionIndex=Math.min(sectionIndex,doc.sections.length-1);elementIndex=-1;change();render();};
 $('add-section').onclick=()=>{if(doc.sections.length>=30)return msg('El diseño admite hasta 30 secciones.',true);mutate(()=>{doc.sections.push({name:'Nueva sección',height:700,background:'#fff8eb',backgroundImage:'',elements:[]});sectionIndex=doc.sections.length-1;elementIndex=-1;});};
 $('design-blank').onclick=()=>mutate(()=>{doc={sections:[{name:'Portada',height:900,background:'#fff8eb',backgroundImage:'',elements:[]}]};sectionIndex=0;elementIndex=-1;});
 $('element-section').onchange=event=>{const to=Number(event.target.value);if(to===sectionIndex||!element())return;mutate(()=>{const [e]=section().elements.splice(elementIndex,1);sectionIndex=to;fit(e);section().elements.push(e);elementIndex=section().elements.length-1;});};
 $('section-up').onclick=()=>{if(sectionIndex>0)mutate(()=>{[doc.sections[sectionIndex-1],doc.sections[sectionIndex]]=[doc.sections[sectionIndex],doc.sections[sectionIndex-1]];sectionIndex--;});};
 $('section-down').onclick=()=>{if(sectionIndex<doc.sections.length-1)mutate(()=>{[doc.sections[sectionIndex+1],doc.sections[sectionIndex]]=[doc.sections[sectionIndex],doc.sections[sectionIndex+1]];sectionIndex++;});};
 $('section-copy').onclick=()=>{if(doc.sections.length>=30)return;mutate(()=>{doc.sections.splice(sectionIndex+1,0,structuredClone(section()));sectionIndex++;elementIndex=-1;});};
 $('section-remove').onclick=()=>{if(doc.sections.length===1)return msg('Dejá al menos una sección.',true);mutate(()=>{doc.sections.splice(sectionIndex,1);sectionIndex=Math.min(sectionIndex,doc.sections.length-1);elementIndex=-1;});};
 for(const [id,key]of Object.entries({'section-name':'name','section-height':'height','section-image':'backgroundImage'}))$(id).addEventListener('change',event=>mutate(()=>{section()[key]=key==='height'?clamp(Number(event.target.value)||700,200,2400):event.target.value;section().elements.forEach(fit);}));
 function canAdd(){if(doc.sections.reduce((n,s)=>n+s.elements.length,0)>=300){msg('El diseño admite hasta 300 elementos.',true);return false;}return true;}
 root.querySelectorAll('[data-add]').forEach(b=>b.onclick=()=>{if(!canAdd())return;mutate(()=>{section().elements.push({type:b.dataset.add,text:b.dataset.add==='title'?'Tu título':b.dataset.add==='button'?'Ver más':b.dataset.add==='image'?'':'Tu texto',x:10,y:40,width:80,height:Math.min(140,section().height-40),fontSize:b.dataset.add==='title'?52:28,font:'Georgia, serif',color:'#365d53',background:'#f1dfc4',align:'center',image:'',link:''});elementIndex=section().elements.length-1;});});
 const fields={'element-text':'text','element-image':'image','element-link':'link','element-font':'font','element-size':'fontSize','element-align':'align','element-x':'x','element-y':'y','element-width':'width','element-height':'height'};
 for(const [id,key]of Object.entries(fields))$(id).addEventListener('change',event=>{if(!element())return;mutate(()=>{let value=event.target.value;if(['x','y','width','height','fontSize'].includes(key))value=Number(value)||0;element()[key]=value;if(key==='fontSize')element().fontSize=clamp(value,10,160);fit(element());});});
 $('element-copy').onclick=()=>{if(!element()||!canAdd())return;mutate(()=>{const e=structuredClone(element());e.y+=20;fit(e);section().elements.push(e);elementIndex=section().elements.length-1;});};
 $('element-remove').onclick=()=>{if(element())mutate(()=>{section().elements.splice(elementIndex,1);elementIndex=-1;});};
 $('element-front').onclick=()=>{if(element())mutate(()=>{const [e]=section().elements.splice(elementIndex,1);section().elements.push(e);elementIndex=section().elements.length-1;});};
 $('element-back').onclick=()=>{if(element())mutate(()=>{const [e]=section().elements.splice(elementIndex,1);section().elements.unshift(e);elementIndex=0;});};
 function normalizeDesignColor(value){
  const hex=value.trim().replace(/^#/,'');
  if(!/^(?:[a-f0-9]{3}|[a-f0-9]{6})$/i.test(hex))return null;
  return '#'+(hex.length===3?[...hex].map(c=>c+c).join(''):hex).toUpperCase();
 }
 function bindDesignColor(id,getValue,setValue){
  const picker=$(id),hex=root.querySelector(`[data-design-color-for="${id}"]`);let editing=false;
  const clearError=()=>{hex.setCustomValidity('');hex.removeAttribute('aria-invalid');};
  const paint=value=>{picker.value=value;picker.style.backgroundColor=value;};
  const update=value=>{
   if((getValue()||'').toUpperCase()===value)return;
   if(!editing){remember();editing=true;}setValue(value);change();renderStage();
  };
  colorControls.push({sync(){editing=false;clearError();const value=normalizeDesignColor(getValue()||'#FFFFFF');paint(value||'#FFFFFF');hex.value=value||'#FFFFFF';}});
  const pick=commit=>{const value=normalizeDesignColor(picker.value);clearError();paint(value);hex.value=value;update(value);if(commit)editing=false;};
  picker.addEventListener('input',()=>pick(false));picker.addEventListener('change',()=>pick(true));
  const type=commit=>{
   const value=normalizeDesignColor(hex.value);
   if(!value){hex.setCustomValidity('Escribí un código de 3 o 6 caracteres, por ejemplo #C66FFF.');hex.setAttribute('aria-invalid','true');return;}
   clearError();paint(value);update(value);if(commit){hex.value=value;editing=false;}
  };
  hex.addEventListener('input',()=>type(false));hex.addEventListener('change',()=>type(true));hex.addEventListener('blur',()=>type(true));
 }
 bindDesignColor('section-bg',()=>section().background,value=>{section().background=value;});
 bindDesignColor('element-color',()=>element()?.color,value=>{if(element())element().color=value;});
 bindDesignColor('element-bg',()=>element()?.background,value=>{if(element())element().background=value;});
 function checkDesignColors(){
  for(const input of root.querySelectorAll('[data-design-color-hex]')){
   if(!input.closest('[hidden]')&&!input.checkValidity()){input.reportValidity();throw new Error('Revisá el código hexadecimal del color antes de guardar.');}
  }
 }
 async function request(path,body,json=false){const response=await fetch(root.dataset.url+path,{method:'POST',headers:{RequestVerificationToken:token,...(json?{'Content-Type':'application/json'}:{})},body:json?JSON.stringify(body):body});if(response.redirected)throw new Error('La sesión terminó. Volvé a iniciar sesión para guardar.');if(!response.ok){let text='No se pudo guardar. Revisá la conexión e intentá de nuevo.';try{text=(await response.json()).error||text;}catch{}throw new Error(text);}return response;}
 async function save(){checkDesignColors();const savedRevision=revision;const result=await(await request('',{document:doc,version},true)).json();version=result.version;if(revision===savedRevision){dirty=false;msg('Borrador guardado. La invitación publicada conserva su diseño.');}else msg('Se guardó la versión anterior. Guardá los últimos cambios.');}
 $('design-save').onclick=async()=>{const button=$('design-save');button.disabled=true;try{await save();}catch(e){msg(e.message,true);}finally{button.disabled=false;}};
 $('design-preview').onclick=event=>{if(dirty){event.preventDefault();msg('Guardá el borrador para ver los últimos cambios en la vista previa.',true);}};
 for(const [id,enabled]of [['design-activate',true],['design-deactivate',false]]){if(!$(id))continue;$(id).onclick=async()=>{const button=$(id);button.disabled=true;try{await save();const body=new FormData();body.set('enabled',String(enabled));await request('/activate',body);version++;msg(enabled?'Diseño aplicado a la invitación.':'La invitación vuelve a usar su diseño anterior.');}catch(e){msg(e.message,true);}finally{button.disabled=false;}};}
 function imageOptions(){for(const id of ['section-image','element-image']){const input=$(id),value=input.value;input.replaceChildren(new Option('Sin imagen',''));assets.forEach(url=>input.add(new Option(decodeURIComponent(url.split('/').pop()),url)));input.value=value;}inspector();}
 $('design-upload').onchange=async event=>{const file=event.target.files[0];if(!file)return;if(file.size>10*1024*1024)return msg('Elegí una imagen de hasta 10 MB.',true);event.target.disabled=true;try{const body=new FormData();body.set('file',file);const result=await(await request('/upload',body)).json();assets.push(result.url);imageOptions();if(element()?.type==='image')mutate(()=>{element().image=result.url;});msg('Imagen subida. Seleccionala en el elemento o en el fondo.');}catch(e){msg(e.message,true);}finally{event.target.disabled=false;}};
 window.addEventListener('beforeunload',event=>{if(dirty){event.preventDefault();event.returnValue='';}});
 loadFontLibrary([]);render();
 fetch(root.dataset.url+'/assets').then(r=>{if(!r.ok)throw new Error();return r.json();}).then(result=>{assets=[...new Set([...result.images,...doc.sections.flatMap(s=>[s.backgroundImage,...s.elements.map(e=>e.image)])].filter(Boolean))];imageOptions();loadFontLibrary(result.fonts||[]);}).catch(()=>msg('No se pudo cargar la biblioteca de imágenes. Podés seguir editando y volver a intentar más tarde.',true));
})();
