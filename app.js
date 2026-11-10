(() => {
  const games = [
    {id:"sm64",title:"Super Mario 64",system:"Nintendo 64",year:"1996",genre:"Platformer",tag:"N64",mark:"64",colors:["#2b7b53","#102c25"],description:"A landmark 3D platform adventure and a great example of early 3D game design."},
    {id:"oot",title:"The Legend of Zelda: Ocarina of Time",system:"Nintendo 64",year:"1998",genre:"Adventure",tag:"N64",mark:"ZELDA",colors:["#8a7138","#283a27"],description:"A fantasy adventure built around exploration, puzzles, music, and time."},
    {id:"goldeneye",title:"GoldenEye 007",system:"Nintendo 64",year:"1997",genre:"Action",tag:"N64",mark:"007",colors:["#6d7b85","#202a32"],description:"A classic console first-person action game from the Nintendo 64 era."},
    {id:"crash",title:"Crash Bandicoot",system:"PlayStation",year:"1996",genre:"Platformer",tag:"PS1",mark:"CRASH",colors:["#c56d2b","#532c28"],description:"A colorful platform adventure from the original PlayStation generation."},
    {id:"spyro",title:"Spyro the Dragon",system:"PlayStation",year:"1998",genre:"Platformer",tag:"PS1",mark:"SPYRO",colors:["#7653a5","#292b53"],description:"A bright 3D adventure centered on exploring worlds and collecting treasure."},
    {id:"metroid",title:"Metroid Fusion",system:"Game Boy Advance",year:"2002",genre:"Action",tag:"GBA",mark:"FUSION",colors:["#397e8d","#152d48"],description:"A sci-fi action adventure designed for the Game Boy Advance."}
  ];
  const toast = document.getElementById('toast');
  const grid = document.getElementById('game-grid');
  const search = document.getElementById('game-search');
  const dialog = document.getElementById('game-dialog');
  const fileInput = document.getElementById('game-file-input');
  const personalGrid = document.getElementById('personal-game-grid');
  const recentGrid = document.getElementById('recent-game-grid');
  const recentEmpty = document.getElementById('recent-empty');
  const personalSearch = document.getElementById('my-game-search');
  const personalSort = document.getElementById('my-game-sort');
  const personalNoResults = document.getElementById('personal-no-results');
  const emptyLibrary = document.getElementById('empty-library');
  const homeShell = document.getElementById('home-shell');
  const emulatorPanel = document.getElementById('emulator-panel');
  const emulatorScreen = document.getElementById('emulator-screen');
  const settingsDialog = document.getElementById('settings-dialog');
  const tvModeButton = document.getElementById('tv-mode-toggle');
  const settingsFullscreenButton = document.getElementById('settings-fullscreen');
  const largeInterfaceSetting = document.getElementById('large-interface-setting');
  const reduceMotionSetting = document.getElementById('reduce-motion-setting');
  const controllerStatus = document.getElementById('settings-controller-status');
  let toastTimer, activeFilter='All', selectedGame=null, database=null;
  const favorites = new Set(JSON.parse(localStorage.getItem('rp-xbox-favorites') || '[]'));

  function notify(message){
    toast.textContent=message;toast.classList.add('show');
    clearTimeout(toastTimer);toastTimer=setTimeout(()=>toast.classList.remove('show'),3000);
  }
  function openDatabase(){
    return new Promise((resolve,reject)=>{
      if(!('indexedDB' in window)){reject(new Error('This browser does not support local game storage.'));return}
      const request=indexedDB.open('RetroPlayerXboxLibrary',1);
      request.onupgradeneeded=()=>{
        const db=request.result;
        if(!db.objectStoreNames.contains('games'))db.createObjectStore('games',{keyPath:'id'});
      };
      request.onsuccess=()=>resolve(request.result);
      request.onerror=()=>reject(request.error||new Error('Could not open game library storage.'));
    });
  }
  function storeGame(record){
    return new Promise((resolve,reject)=>{
      const tx=database.transaction('games','readwrite');
      tx.objectStore('games').put(record);
      tx.oncomplete=()=>resolve();
      tx.onerror=()=>reject(tx.error||new Error('Could not save this game.'));
      tx.onabort=()=>reject(tx.error||new Error('Game save was cancelled.'));
    });
  }
  function readGames(){
    return new Promise((resolve,reject)=>{
      const request=database.transaction('games','readonly').objectStore('games').getAll();
      request.onsuccess=()=>resolve(request.result||[]);
      request.onerror=()=>reject(request.error||new Error('Could not read your library.'));
    });
  }
  function readGame(id){
    return new Promise((resolve,reject)=>{
      const request=database.transaction('games','readonly').objectStore('games').get(id);
      request.onsuccess=()=>resolve(request.result||null);
      request.onerror=()=>reject(request.error||new Error('Could not open this game.'));
    });
  }
  function removeGame(id){
    return new Promise((resolve,reject)=>{
      const tx=database.transaction('games','readwrite');
      tx.objectStore('games').delete(id);
      tx.oncomplete=resolve;
      tx.onerror=()=>reject(tx.error||new Error('Could not remove this game.'));
    });
  }
  function formatSize(bytes){
    if(bytes<1024*1024)return (bytes/1024).toFixed(0)+' KB';
    return (bytes/(1024*1024)).toFixed(1)+' MB';
  }
  function safeId(){
    return window.crypto&&crypto.randomUUID?crypto.randomUUID():Date.now().toString(36)+Math.random().toString(36).slice(2);
  }
  async function renderPersonalLibrary(){
    if(!database)return;
    try{
      const entries=await readGames();
      const term=personalSearch.value.trim().toLowerCase();
      const sort=personalSort.value;
      const sorted=[...entries].sort((a,b)=>{
        if(sort==='title')return a.title.localeCompare(b.title);
        if(sort==='size')return (b.size||0)-(a.size||0);
        if(sort==='played')return (b.lastPlayed||0)-(a.lastPlayed||0);
        return (b.addedAt||0)-(a.addedAt||0);
      });
      const visible=sorted.filter(entry=>(entry.title+' '+entry.fileName).toLowerCase().includes(term));
      personalGrid.innerHTML=visible.map(entry=>`<article class="personal-game-card">
        <div class="personal-game-art"><span>PS</span><small>PLAYSTATION</small></div>
        <div class="personal-game-info"><h3>${escapeHtml(entry.title)}</h3><p>${escapeHtml(entry.fileName)} · ${formatSize(entry.size)}</p><span class="game-badges"><span>PS1 IMAGE</span><span>ON THIS DEVICE</span></span></div>
        <div class="personal-game-actions"><button class="primary-btn focusable" data-play="${entry.id}">▶ Play</button><button class="secondary-btn focusable" data-remove="${entry.id}" aria-label="Remove ${escapeHtml(entry.title)}">Remove</button></div>
      </article>`).join('');
      emptyLibrary.hidden=entries.length!==0;
      personalNoResults.hidden=entries.length===0||visible.length!==0;
      personalGrid.querySelectorAll('[data-play]').forEach(button=>button.addEventListener('click',()=>launchSavedGame(button.dataset.play)));
      personalGrid.querySelectorAll('[data-remove]').forEach(button=>button.addEventListener('click',()=>deleteSavedGame(button.dataset.remove)));
      const recent=entries.filter(entry=>entry.lastPlayed).sort((a,b)=>b.lastPlayed-a.lastPlayed).slice(0,4);
      recentGrid.innerHTML=recent.map(entry=>`<article class="personal-game-card recent-game-card">
        <div class="personal-game-art"><span>PS</span><small>PLAYSTATION</small></div>
        <div class="personal-game-info"><h3>${escapeHtml(entry.title)}</h3><p>Last played ${new Date(entry.lastPlayed).toLocaleString()} · ${entry.playCount||1} session${(entry.playCount||1)===1?'':'s'}</p><span class="game-badges"><span>RECENTLY PLAYED</span></span></div>
        <div class="personal-game-actions"><button class="primary-btn focusable" data-recent-play="${entry.id}">▶ Resume</button></div>
      </article>`).join('');
      recentEmpty.hidden=recent.length!==0;
      recentGrid.querySelectorAll('[data-recent-play]').forEach(button=>button.addEventListener('click',()=>launchSavedGame(button.dataset.recentPlay)));
    }catch(error){notify(error.message)}
  }
  function escapeHtml(value){
    return String(value).replace(/[&<>"']/g,char=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[char]));
  }
  async function importGame(file){
    const allowed=['bin','iso','img','pbp','chd'];
    const extension=(file.name.split('.').pop()||'').toLowerCase();
    if(!allowed.includes(extension)){notify('That file type is not supported by this first PS1 importer.');return}
    if(file.size===0){notify('That file is empty. Choose a game image file.');return}
    if(file.size>1024*1024*1024){notify('This first version supports files up to 1 GB.');return}
    try{
      if(!database)database=await openDatabase();
      if(navigator.storage&&navigator.storage.persist)await navigator.storage.persist();
      const title=file.name.replace(/\.[^.]+$/,'').replace(/[_-]+/g,' ').trim()||'PlayStation game';
      await storeGame({id:safeId(),title,fileName:file.name,size:file.size,type:file.type||'application/octet-stream',blob:file,lastPlayed:0,addedAt:Date.now()});
      await renderPersonalLibrary();
      notify('Added '+title+' to My games.');
    }catch(error){
      notify('Could not save the file: '+(error.message||'Storage may be full.'));
    }
  }
  async function deleteSavedGame(id){
    try{await removeGame(id);await renderPersonalLibrary();notify('Game removed from this device.')}
    catch(error){notify(error.message)}
  }
  async function launchSavedGame(id){
    try{
      const entry=await readGame(id);
      if(!entry){notify('That game is no longer in your library.');return}
      const file=new File([entry.blob],entry.fileName,{type:entry.type||'application/octet-stream'});
      startPs1Emulator(file,entry.title);
      entry.lastPlayed=Date.now();
      entry.playCount=(entry.playCount||0)+1;
      await storeGame(entry);
      await renderPersonalLibrary();
    }catch(error){notify('Could not open the game: '+(error.message||'Unknown error.'))}
  }
  function startPs1Emulator(file,title){
    homeShell.hidden=true;
    emulatorPanel.hidden=false;
    document.body.classList.add('emulator-active');
    document.getElementById('emulator-game-title').textContent=title;
    document.getElementById('emulator-status').textContent='Loading the PlayStation emulator…';
    emulatorScreen.innerHTML='<div class="emulator-loading">Preparing your game…</div>';
    window.EJS_player='#emulator-screen';
    window.EJS_gameName=title;
    window.EJS_gameUrl=file;
    window.EJS_core='psx';
    window.EJS_pathtodata='https://cdn.emulatorjs.org/stable/data/';
    window.EJS_startOnLoaded=true;
    window.EJS_Buttons={playPause:true,volume:true,fullscreen:true,settings:true};
    const script=document.createElement('script');
    script.src='https://cdn.emulatorjs.org/stable/data/loader.js';
    script.async=true;
    script.onerror=()=>{
      document.getElementById('emulator-status').textContent='The emulator loader could not be downloaded. Check the Xbox internet connection and try again.';
      notify('Could not load the emulator. Check your connection.');
    };
    script.onload=()=>{document.getElementById('emulator-status').textContent='Emulator loaded. Use your Xbox controller to play.'};
    document.body.appendChild(script);
  }
  function exitEmulator(){
    // Reloading clears the running WebAssembly core; the IndexedDB library remains saved.
    window.location.reload();
  }
  function render(){
    const term=search.value.trim().toLowerCase();
    const visible=games.filter(g=>(activeFilter==='All'||g.system===activeFilter)&&(!term||(g.title+' '+g.system+' '+g.genre).toLowerCase().includes(term)));
    grid.innerHTML=visible.map(g=>`<button class="game-card focusable" data-game="${g.id}" aria-label="View ${g.title} details">
      <span class="cover" style="--cover-a:${g.colors[0]};--cover-b:${g.colors[1]}"><span>${g.mark}</span></span>
      <span class="game-info"><h3>${g.title}</h3><p>${g.system} · ${g.year}</p><span class="game-badges"><span>${g.genre}</span><span>SAMPLE ENTRY</span>${favorites.has(g.id)?'<span>★ SAVED</span>':''}</span></span>
    </button>`).join('');
    document.getElementById('no-results').hidden=visible.length!==0;
    document.getElementById('result-count').textContent=visible.length+' SAMPLE '+(visible.length===1?'ENTRY':'ENTRIES');
    grid.querySelectorAll('[data-game]').forEach(btn=>btn.addEventListener('click',()=>openGame(btn.dataset.game)));
  }
  function setFilter(filter){
    activeFilter=filter;
    document.querySelectorAll('[data-filter]').forEach(btn=>{const active=btn.dataset.filter===filter;btn.classList.toggle('active',active);btn.setAttribute('aria-pressed',String(active))});
    render();document.getElementById('library').scrollIntoView({behavior:'smooth'});
  }
  function openGame(id){
    selectedGame=games.find(g=>g.id===id);if(!selectedGame)return;
    document.getElementById('dialog-system').textContent=selectedGame.system.toUpperCase();
    document.getElementById('dialog-title').textContent=selectedGame.title;
    document.getElementById('dialog-description').textContent=selectedGame.description;
    document.getElementById('dialog-meta').innerHTML=[selectedGame.year,selectedGame.genre,'Sample catalog entry'].map(x=>'<span>'+x+'</span>').join('');
    updateFavoriteButton();dialog.showModal();
  }
  function updateFavoriteButton(){
    const saved=favorites.has(selectedGame.id);
    document.getElementById('favorite-game').textContent=saved?'★ Remove favorite':'☆ Add to favorites';
  }
  document.querySelectorAll('[data-filter]').forEach(btn=>btn.addEventListener('click',()=>setFilter(btn.dataset.filter)));
  document.querySelectorAll('[data-system-filter]').forEach(btn=>btn.addEventListener('click',()=>setFilter(btn.dataset.systemFilter)));
  document.querySelector('[data-action="library"]').addEventListener('click',()=>document.getElementById('my-library').scrollIntoView({behavior:'smooth'}));
  search.addEventListener('input',render);
  personalSearch.addEventListener('input',renderPersonalLibrary);
  personalSort.addEventListener('change',renderPersonalLibrary);
  document.getElementById('import-game').addEventListener('click',()=>fileInput.click());
  fileInput.addEventListener('change',async event=>{
    const file=event.target.files&&event.target.files[0];
    if(file)await importGame(file);
    fileInput.value='';
  });
  document.getElementById('exit-emulator').addEventListener('click',exitEmulator);
  document.getElementById('dialog-close').addEventListener('click',()=>dialog.close());
  document.getElementById('launch-game').addEventListener('click',()=>{
    dialog.close();document.getElementById('import-game').focus();fileInput.click();
  });
  document.getElementById('favorite-game').addEventListener('click',()=>{
    if(!selectedGame)return;
    if(favorites.has(selectedGame.id))favorites.delete(selectedGame.id);else favorites.add(selectedGame.id);
    localStorage.setItem('rp-xbox-favorites',JSON.stringify([...favorites]));updateFavoriteButton();render();
  });
  dialog.addEventListener('click',e=>{if(e.target===dialog)dialog.close()});

  function applyDisplayPreferences(){
    document.body.classList.toggle('large-interface',largeInterfaceSetting.checked);
    document.body.classList.toggle('reduce-motion',reduceMotionSetting.checked);
    localStorage.setItem('rp-xbox-large-interface',String(largeInterfaceSetting.checked));
    localStorage.setItem('rp-xbox-reduce-motion',String(reduceMotionSetting.checked));
  }
  function syncFullscreenState(){
    const active=!!document.fullscreenElement||document.body.classList.contains('tv-mode-fallback');
    tvModeButton.setAttribute('aria-pressed',String(active));
    tvModeButton.classList.toggle('is-active',active);
    settingsFullscreenButton.querySelector('.setting-arrow').textContent=active?'↙':'↗';
  }
  async function toggleTvMode(){
    try{
      if(document.fullscreenElement){await document.exitFullscreen();document.body.classList.remove('tv-mode-fallback');notify('Fullscreen mode off.')}
      else if(document.documentElement.requestFullscreen){await document.documentElement.requestFullscreen();document.body.classList.remove('tv-mode-fallback');notify('TV mode enabled.')}
      else{document.body.classList.toggle('tv-mode-fallback');notify(document.body.classList.contains('tv-mode-fallback')?'TV layout enabled.':'TV layout disabled.')}
    }catch(error){document.body.classList.toggle('tv-mode-fallback');notify(document.body.classList.contains('tv-mode-fallback')?'TV layout enabled. Fullscreen is unavailable here.':'TV layout disabled.')}
    syncFullscreenState();
  }
  async function refreshSettingsStats(){
    try{const entries=database?await readGames():[];document.getElementById('settings-game-count').textContent=String(entries.length)}
    catch(error){document.getElementById('settings-game-count').textContent='Unavailable'}
    if(navigator.storage&&navigator.storage.estimate){
      try{const estimate=await navigator.storage.estimate(),used=estimate.usage||0,quota=estimate.quota||0;
        const format=bytes=>bytes<1024*1024?(bytes/1024).toFixed(0)+' KB':(bytes/(1024*1024)).toFixed(1)+' MB';
        document.getElementById('settings-storage-used').textContent=quota?format(used)+' / '+format(quota):format(used);
      }catch(error){document.getElementById('settings-storage-used').textContent='Unavailable'}
    }else document.getElementById('settings-storage-used').textContent='Not supported';
  }
  function openSettings(){
    largeInterfaceSetting.checked=localStorage.getItem('rp-xbox-large-interface')==='true';
    reduceMotionSetting.checked=localStorage.getItem('rp-xbox-reduce-motion')==='true';
    applyDisplayPreferences();refreshSettingsStats();settingsDialog.showModal();document.getElementById('settings-close').focus();
  }
  document.getElementById('open-settings').addEventListener('click',openSettings);
  document.getElementById('settings-close').addEventListener('click',()=>settingsDialog.close());
  document.getElementById('settings-done').addEventListener('click',()=>settingsDialog.close());
  tvModeButton.addEventListener('click',toggleTvMode);
  settingsFullscreenButton.addEventListener('click',toggleTvMode);
  largeInterfaceSetting.addEventListener('change',applyDisplayPreferences);
  reduceMotionSetting.addEventListener('change',applyDisplayPreferences);
  settingsDialog.addEventListener('click',event=>{if(event.target===settingsDialog)settingsDialog.close()});
  document.addEventListener('fullscreenchange',syncFullscreenState);
  window.addEventListener('gamepadconnected',event=>{controllerStatus.textContent='Connected: '+event.gamepad.id});
  window.addEventListener('gamepaddisconnected',()=>{controllerStatus.textContent='Waiting for a controller'});
  largeInterfaceSetting.checked=localStorage.getItem('rp-xbox-large-interface')==='true';
  reduceMotionSetting.checked=localStorage.getItem('rp-xbox-reduce-motion')==='true';
  applyDisplayPreferences();

  // Controller-friendly focus movement using the browser Gamepad API.
  const selector='button,a[href],input,[tabindex]:not([tabindex="-1"])';
  let lastDirection=0,lastButtons={};
  function items(){return [...document.querySelectorAll(selector)].filter(el=>!el.disabled&&el.offsetParent!==null)}
  function moveFocus(dx,dy){
    const list=items();if(!list.length)return;const current=document.activeElement;
    const origin=current&&current.getBoundingClientRect?current.getBoundingClientRect():null;
    if(!origin||!list.includes(current)){list[0].focus();return}
    const ox=origin.left+origin.width/2,oy=origin.top+origin.height/2;let best=null,bestScore=Infinity;
    for(const el of list){if(el===current)continue;const r=el.getBoundingClientRect(),x=r.left+r.width/2,y=r.top+r.height/2;const primary=dx?(x-ox)*dx:(y-oy)*dy;if(primary<=4)continue;const score=primary+Math.abs(dx?y-oy:x-ox)*2.2;if(score<bestScore){bestScore=score;best=el}}
    if(best)best.focus();
  }
  let lastPadId='';
  function updateWebControllerStatus(pad){
    const id=pad?.id||'';
    if(id!==lastPadId){
      lastPadId=id;
      if(controllerStatus)controllerStatus.textContent=pad?'Connected: '+(id||'Gamepad'):'Waiting for a controller';
    }
  }
  function poll(){
    // The native Xbox shell owns dashboard navigation. Leave the browser Gamepad API
    // available to EmulatorJS while a game is running; do not send duplicate UI input.
    if(window.RetroPlayerNativeHost){
      requestAnimationFrame(poll);
      return;
    }
    const pads=navigator.getGamepads?[...navigator.getGamepads()]:[];
    const pad=pads.find(p=>p&&p.connected);
    updateWebControllerStatus(pad);
    if(pad){
      const pressed=i=>!!pad.buttons[i]?.pressed;
      const ax=Math.abs(pad.axes[0]||0)>.55?Math.sign(pad.axes[0]):0;
      const ay=Math.abs(pad.axes[1]||0)>.55?Math.sign(pad.axes[1]):0;
      if((ax||ay)&&!lastDirection)moveFocus(ax,ay);
      lastDirection=ax||ay;
      if(pressed(0)&&!lastButtons.a)window.RetroPlayerNativeInput('A');
      if(pressed(1)&&!lastButtons.b)window.RetroPlayerNativeInput('B');
      if(pressed(2)&&!lastButtons.x)window.RetroPlayerNativeInput('X');
      if(pressed(3)&&!lastButtons.y)window.RetroPlayerNativeInput('Y');
      if(pressed(4)&&!lastButtons.lb)window.RetroPlayerNativeInput('LB');
      if(pressed(5)&&!lastButtons.rb)window.RetroPlayerNativeInput('RB');
      if(pressed(6)&&!lastButtons.lt)window.RetroPlayerNativeInput('LT');
      if(pressed(7)&&!lastButtons.rt)window.RetroPlayerNativeInput('RT');
      if(pressed(12)&&!lastButtons.up)moveFocus(0,-1);
      if(pressed(13)&&!lastButtons.down)moveFocus(0,1);
      if(pressed(14)&&!lastButtons.left)moveFocus(-1,0);
      if(pressed(15)&&!lastButtons.right)moveFocus(1,0);
      lastButtons={a:pressed(0),b:pressed(1),x:pressed(2),y:pressed(3),lb:pressed(4),rb:pressed(5),lt:pressed(6),rt:pressed(7),up:pressed(12),down:pressed(13),left:pressed(14),right:pressed(15)};
    }else{
      lastDirection=0;
      lastButtons={};
    }
    requestAnimationFrame(poll);
  }
  window.addEventListener('gamepadconnected',e=>notify('Controller connected: '+e.gamepad.id));
  window.addEventListener('gamepaddisconnected',()=>notify('Controller disconnected.'));
  if('getGamepads'in navigator)requestAnimationFrame(poll);
  document.addEventListener('keydown',e=>{if(['ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(e.key)&&!['INPUT'].includes(document.activeElement?.tagName)){e.preventDefault();moveFocus(e.key==='ArrowLeft'?-1:e.key==='ArrowRight'?1:0,e.key==='ArrowUp'?-1:e.key==='ArrowDown'?1:0)}});
  // Native UWP host bridge. The host owns dashboard input on Xbox; the browser
  // Gamepad API remains untouched so EmulatorJS can read gameplay input itself.
  window.RetroPlayerNativeHost=false;
  window.RetroPlayerNativeControllerStatus=function(status){
    if(controllerStatus)controllerStatus.textContent=status;
  };
  window.RetroPlayerNativeInput=function(action){
    window.dispatchEvent(new CustomEvent('retroplayer:gamepad',{detail:{action}}));
    if(action==='A'){document.activeElement?.click();return}
    if(action==='B'){
      if(dialog.open)dialog.close();
      else if(document.activeElement&&document.activeElement!==document.body)document.activeElement.blur();
      return;
    }
    if(action==='X'){
      const favorite=document.getElementById('favorite-game');
      if(dialog.open&&favorite)favorite.click();
      return;
    }
    if(action==='Y'){
      if(dialog.open)dialog.close();
      return;
    }
    const key=({ArrowUp:'ArrowUp',ArrowDown:'ArrowDown',ArrowLeft:'ArrowLeft',ArrowRight:'ArrowRight'})[action];
    if(key)document.dispatchEvent(new KeyboardEvent('keydown',{key,bubbles:true,cancelable:true}));
  };
  render();
  openDatabase().then(db=>{database=db;renderPersonalLibrary()}).catch(error=>notify('Local library storage unavailable: '+error.message));
})();