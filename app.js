(() => {
  const games = [
    {id:"sm64",title:"Super Mario 64",system:"Nintendo 64",year:"1996",genre:"Platformer",tag:"N64",mark:"64",colors:["#2b7b53","#102c25"],description:"A landmark 3D platform adventure and a great example of early 3D game design."},
    {id:"oot",title:"The Legend of Zelda: Ocarina of Time",system:"Nintendo 64",year:"1998",genre:"Adventure",tag:"N64",mark:"ZELDA",colors:["#8a7138","#283a27"],description:"A fantasy adventure built around exploration, puzzles, music, and time."},
    {id:"goldeneye",title:"GoldenEye 007",system:"Nintendo 64",year:"1997",genre:"Action",tag:"N64",mark:"007",colors:["#6d7b85","#202a32"],description:"A classic console first-person action game from the Nintendo 64 era."},
    {id:"crash",title:"Crash Bandicoot",system:"PlayStation",year:"1996",genre:"Platformer",tag:"PS1",mark:"CRASH",colors:["#c56d2b","#532c28"],description:"A colorful platform adventure from the original PlayStation generation."},
    {id:"spyro",title:"Spyro the Dragon",system:"PlayStation",year:"1998",genre:"Platformer",tag:"PS1",mark:"SPYRO",colors:["#7653a5","#292b53"],description:"A bright 3D adventure centered on exploring worlds and collecting treasure."},
    {id:"metroid",title:"Metroid Fusion",system:"Game Boy Advance",year:"2002",genre:"Action",tag:"GBA",mark:"FUSION",colors:["#397e8d","#152d48"],description:"A sci-fi action adventure designed for the Game Boy Advance."}
  ];
  const toast = document.getElementById('toast'), grid = document.getElementById('game-grid'), search = document.getElementById('game-search');
  const dialog = document.getElementById('game-dialog');
  let toastTimer, activeFilter='All', selectedGame=null;
  const favorites = new Set(JSON.parse(localStorage.getItem('rp-xbox-favorites') || '[]'));
  function notify(message){toast.textContent=message;toast.classList.add('show');clearTimeout(toastTimer);toastTimer=setTimeout(()=>toast.classList.remove('show'),2500)}
  function render(){
    const term=search.value.trim().toLowerCase();
    const visible=games.filter(g=>(activeFilter==='All'||g.system===activeFilter)&&(!term||(g.title+' '+g.system+' '+g.genre).toLowerCase().includes(term)));
    grid.innerHTML=visible.map(g=>`<button class="game-card focusable" data-game="${g.id}" aria-label="View ${g.title} details">
      <span class="cover" style="--cover-a:${g.colors[0]};--cover-b:${g.colors[1]}"><span>${g.mark}</span></span>
      <span class="game-info"><h3>${g.title}</h3><p>${g.system} · ${g.year}</p><span class="game-badges"><span>${g.genre}</span><span>DEMO ENTRY</span>${favorites.has(g.id)?'<span>★ SAVED</span>':''}</span></span>
    </button>`).join('');
    document.getElementById('no-results').hidden=visible.length!==0;
    document.getElementById('result-count').textContent=visible.length+' SAMPLE '+(visible.length===1?'ENTRY':'ENTRIES');
    grid.querySelectorAll('[data-game]').forEach(btn=>btn.addEventListener('click',()=>openGame(btn.dataset.game)));
  }
  function setFilter(filter){
    activeFilter=filter;
    document.querySelectorAll('[data-filter]').forEach(btn=>{const active=btn.dataset.filter===filter;btn.classList.toggle('active',active);btn.setAttribute('aria-pressed',String(active))});
    render();
    document.getElementById('library').scrollIntoView({behavior:'smooth'});
  }
  function openGame(id){
    selectedGame=games.find(g=>g.id===id);if(!selectedGame)return;
    document.getElementById('dialog-system').textContent=selectedGame.system.toUpperCase();
    document.getElementById('dialog-title').textContent=selectedGame.title;
    document.getElementById('dialog-description').textContent=selectedGame.description;
    document.getElementById('dialog-meta').innerHTML=[selectedGame.year,selectedGame.genre,'Sample catalog entry'].map(x=>'<span>'+x+'</span>').join('');
    updateFavoriteButton();dialog.showModal();
  }
  function updateFavoriteButton(){const saved=favorites.has(selectedGame.id);document.getElementById('favorite-game').textContent=saved?'★ Remove favorite':'☆ Add to favorites'}
  document.querySelectorAll('[data-filter]').forEach(btn=>btn.addEventListener('click',()=>setFilter(btn.dataset.filter)));
  document.querySelectorAll('[data-system-filter]').forEach(btn=>btn.addEventListener('click',()=>setFilter(btn.dataset.systemFilter)));
  document.querySelector('[data-action="library"]').addEventListener('click',()=>document.getElementById('library').scrollIntoView({behavior:'smooth'}));
  search.addEventListener('input',render);
  document.getElementById('dialog-close').addEventListener('click',()=>dialog.close());
  document.getElementById('launch-game').addEventListener('click',()=>notify('Launch preview only — emulator integration is not connected yet.'));
  document.getElementById('favorite-game').addEventListener('click',()=>{
    if(!selectedGame)return;
    if(favorites.has(selectedGame.id))favorites.delete(selectedGame.id);else favorites.add(selectedGame.id);
    localStorage.setItem('rp-xbox-favorites',JSON.stringify([...favorites]));updateFavoriteButton();render();
  });
  dialog.addEventListener('click',e=>{if(e.target===dialog)dialog.close()});

  // Controller-friendly focus movement using the browser Gamepad API.
  const selector='button,a[href],input,[tabindex]:not([tabindex="-1"])';let lastDirection=0,lastButtons={};
  function items(){return [...document.querySelectorAll(selector)].filter(el=>!el.disabled&&el.offsetParent!==null)}
  function moveFocus(dx,dy){
    const list=items();if(!list.length)return;const current=document.activeElement;
    const origin=current&&current.getBoundingClientRect?current.getBoundingClientRect():null;
    if(!origin||!list.includes(current)){list[0].focus();return}
    const ox=origin.left+origin.width/2,oy=origin.top+origin.height/2;let best=null,bestScore=Infinity;
    for(const el of list){if(el===current)continue;const r=el.getBoundingClientRect(),x=r.left+r.width/2,y=r.top+r.height/2;const primary=dx?(x-ox)*dx:(y-oy)*dy;if(primary<=4)continue;const score=primary+Math.abs(dx?y-oy:x-ox)*2.2;if(score<bestScore){bestScore=score;best=el}}
    if(best)best.focus();
  }
  function poll(){
    const pad=[...(navigator.getGamepads?navigator.getGamepads():[])].find(Boolean);
    if(pad){
      const pressed=i=>!!pad.buttons[i]?.pressed,ax=Math.abs(pad.axes[0]||0)>.55?Math.sign(pad.axes[0]):0,ay=Math.abs(pad.axes[1]||0)>.55?Math.sign(pad.axes[1]):0;
      if((ax||ay)&&!lastDirection)moveFocus(ax,ay);lastDirection=ax||ay;
      if(pressed(0)&&!lastButtons.a)document.activeElement?.click();
      if(pressed(1)&&!lastButtons.b){if(dialog.open)dialog.close();else if(document.activeElement!==document.body)document.activeElement.blur();else window.scrollTo({top:0,behavior:'smooth'})}
      if(pressed(12)&&!lastButtons.up)moveFocus(0,-1);if(pressed(13)&&!lastButtons.down)moveFocus(0,1);if(pressed(14)&&!lastButtons.left)moveFocus(-1,0);if(pressed(15)&&!lastButtons.right)moveFocus(1,0);
      lastButtons={a:pressed(0),b:pressed(1),up:pressed(12),down:pressed(13),left:pressed(14),right:pressed(15)};
    }else{lastDirection=0;lastButtons={}}
    requestAnimationFrame(poll);
  }
  window.addEventListener('gamepadconnected',e=>notify('Controller connected: '+e.gamepad.id));
  window.addEventListener('gamepaddisconnected',()=>notify('Controller disconnected.'));
  if('getGamepads'in navigator)requestAnimationFrame(poll);
  document.addEventListener('keydown',e=>{if(['ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(e.key)&&!['INPUT'].includes(document.activeElement?.tagName)){e.preventDefault();moveFocus(e.key==='ArrowLeft'?-1:e.key==='ArrowRight'?1:0,e.key==='ArrowUp'?-1:e.key==='ArrowDown'?1:0)}});
  render();
})();