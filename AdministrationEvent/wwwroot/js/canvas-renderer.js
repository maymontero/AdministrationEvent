window.InvitationCanvas = {
  loadedFonts: new Map(),
  fontFamily(url, load = true) {
    let hash = 2166136261;
    for (const char of url) { hash ^= char.charCodeAt(0); hash = Math.imul(hash, 16777619); }
    const family = 'CanvasFont' + (hash >>> 0).toString(36);
    if (load && !this.loadedFonts.has(url)) {
      const face = new FontFace(family, `url("${encodeURI(url)}")`);
      document.fonts.add(face);
      this.loadedFonts.set(url, face.load().catch(() => null));
    }
    return family;
  },
  element(e, date, editing = false) {
    const node = document.createElement(!editing && e.type === 'button' ? 'a' : 'div'); node.className = 'canvas-element canvas-' + e.type;
    Object.assign(node.style, {left:e.x+'%',top:e.y+'px',width:e.width+'%',height:e.height+'px',fontFamily:e.fontUrl?this.fontFamily(e.fontUrl):e.font,fontSize:e.fontSize+'px',color:e.color,textAlign:e.align});
    if(e.type==='image') { if(e.image){const img=document.createElement('img');img.src=e.image;img.alt=e.text||'';img.draggable=false;node.append(img);}else node.textContent=editing?'Elegí una imagen':''; }
    else if(e.type==='countdown') { const days=date?Math.max(0,Math.ceil((Date.parse(date)-Date.now())/86400000)):null;node.textContent=days===null?'Fecha por confirmar':`${days} días`; }
    else node.textContent=e.text;
    if(e.type==='button'){node.style.background=e.background; if(!editing&&e.link){node.href=e.link;node.target='_blank';node.rel='noopener noreferrer';}}
    return node;
  },
  section(s, date, editing = false) {
    const section=document.createElement('section');section.className='canvas-section';section.style.height=s.height+'px';section.style.backgroundColor=s.background;
    if(s.backgroundImage)section.style.backgroundImage=`url("${s.backgroundImage}")`;
    s.elements.forEach(e=>section.append(this.element(e,date,editing)));return section;
  }
};
(() => {
 const target=document.querySelector('#canvas-invitation');if(!target)return; const doc=JSON.parse(document.querySelector('#canvas-document').textContent);
 doc.sections.forEach(s=>target.append(InvitationCanvas.section(s,target.dataset.eventDate)));
 const resize=()=>{const scale=Math.min(1,target.parentElement.clientWidth/900);target.style.transform=`scale(${scale})`;target.parentElement.style.height=(target.scrollHeight*scale)+'px';};new ResizeObserver(resize).observe(target.parentElement);resize();
})();
