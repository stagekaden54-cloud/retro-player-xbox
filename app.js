(() => {
  const toast = document.getElementById('toast');
  let toastTimer;
  function notify(message) {
    toast.textContent = message;
    toast.classList.add('show');
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast.classList.remove('show'), 2600);
  }

  document.querySelectorAll('[data-action="library"]').forEach(button => {
    button.addEventListener('click', () => {
      document.getElementById('library').scrollIntoView({behavior:'smooth'});
      notify('Your game library is being prepared.');
    });
  });

  document.querySelectorAll('[data-system]').forEach(button => {
    button.addEventListener('click', () => notify(button.dataset.system + ' support is planned, but no emulator core is connected yet.'));
  });

  // Browser Gamepad API: basic focus navigation for prototype testing.
  const selector = 'button, a[href], input, [tabindex]:not([tabindex="-1"])';
  let lastDirection = 0;
  let lastButtons = {};
  function focusedItems() { return [...document.querySelectorAll(selector)].filter(el => !el.disabled && el.offsetParent !== null); }
  function moveFocus(dx, dy) {
    const items = focusedItems();
    if (!items.length) return;
    const current = document.activeElement;
    const origin = current && current.getBoundingClientRect ? current.getBoundingClientRect() : null;
    if (!origin || !items.includes(current)) { items[0].focus(); return; }
    const ox = origin.left + origin.width/2, oy = origin.top + origin.height/2;
    let best = null, bestScore = Infinity;
    for (const item of items) {
      if (item === current) continue;
      const r = item.getBoundingClientRect(), x = r.left+r.width/2, y = r.top+r.height/2;
      const primary = dx ? (x-ox)*dx : (y-oy)*dy;
      if (primary <= 4) continue;
      const secondary = dx ? Math.abs(y-oy) : Math.abs(x-ox);
      const score = primary + secondary*2.2;
      if (score < bestScore) { bestScore = score; best = item; }
    }
    if (best) best.focus();
  }
  function pollGamepad() {
    const pads = navigator.getGamepads ? navigator.getGamepads() : [];
    const pad = [...pads].find(Boolean);
    if (pad) {
      const pressed = i => !!pad.buttons[i]?.pressed;
      const axisX = Math.abs(pad.axes[0] || 0) > .55 ? Math.sign(pad.axes[0]) : 0;
      const axisY = Math.abs(pad.axes[1] || 0) > .55 ? Math.sign(pad.axes[1]) : 0;
      const direction = axisX || axisY;
      if (direction && !lastDirection) moveFocus(axisX, axisY);
      lastDirection = direction;
      if (pressed(0) && !lastButtons.a) document.activeElement?.click();
      if (pressed(1) && !lastButtons.b) {
        const active = document.activeElement;
        if (active && active !== document.body) active.blur();
        else window.scrollTo({top:0,behavior:'smooth'});
      }
      if (pressed(12) && !lastButtons.up) moveFocus(0,-1);
      if (pressed(13) && !lastButtons.down) moveFocus(0,1);
      if (pressed(14) && !lastButtons.left) moveFocus(-1,0);
      if (pressed(15) && !lastButtons.right) moveFocus(1,0);
      lastButtons = {a:pressed(0),b:pressed(1),up:pressed(12),down:pressed(13),left:pressed(14),right:pressed(15)};
    } else {
      lastDirection = 0;
      lastButtons = {};
    }
    requestAnimationFrame(pollGamepad);
  }
  window.addEventListener('gamepadconnected', e => notify('Controller connected: ' + e.gamepad.id));
  window.addEventListener('gamepaddisconnected', () => notify('Controller disconnected.'));
  if ('getGamepads' in navigator) requestAnimationFrame(pollGamepad);
  document.addEventListener('keydown', e => {
    if (['ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(e.key)) {
      e.preventDefault();
      moveFocus(e.key==='ArrowLeft'?-1:e.key==='ArrowRight'?1:0,e.key==='ArrowUp'?-1:e.key==='ArrowDown'?1:0);
    }
  });
})();