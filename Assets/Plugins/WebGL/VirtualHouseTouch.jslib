mergeInto(LibraryManager.library, {
  VirtualHouse_InstagramGlobe: function (visible) {
    var panel = document.getElementById('virtual-house-instagram-globe');
    if (!panel && visible) {
      panel = document.createElement('a');
      panel.id = 'virtual-house-instagram-globe';
      panel.href = 'https://www.instagram.com/yuka_mama_triplife/';
      panel.target = '_blank';
      panel.rel = 'noopener noreferrer';
      panel.setAttribute('aria-label', 'Instagram @yuka_mama_triplife（新しいタブで開きます）');
      panel.textContent = '地球儀 · Instagram｜@yuka_mama_triplife を見る ↗';
      panel.style.cssText = 'position:fixed;z-index:10;top:calc(env(safe-area-inset-top,0px) + 116px);left:50%;transform:translateX(-50%);box-sizing:border-box;width:max-content;max-width:calc(100vw - 24px);padding:16px;border:2px solid #e7c89b;border-radius:10px;background:#49372a;color:white;font:16px/1.5 sans-serif;text-align:center;touch-action:manipulation;';
      ['pointerdown', 'pointerup', 'touchstart', 'touchend', 'mousedown', 'mouseup', 'click'].forEach(function (event) {
        panel.addEventListener(event, function (e) { e.stopPropagation(); });
      });
      document.body.appendChild(panel);
    }
    if (panel) panel.style.display = visible ? 'block' : 'none';
  },
  VirtualHouse_TennisGuide: function (visible) {
    var panel = document.getElementById('virtual-house-tennis-area');
    if (!panel && visible) {
      panel = document.createElement('a');
      panel.id = 'virtual-house-tennis-area';
      panel.href = 'https://www.tennisbear.net/circle/119692/info';
      panel.target = '_blank';
      panel.rel = 'noopener noreferrer';
      panel.setAttribute('aria-label', 'テニスサークルを見る（新しいタブで開きます）');
      panel.textContent = '家の北西・テニスコート｜テニスサークルを見る ↗';
      panel.style.cssText = 'position:fixed;z-index:10;top:calc(env(safe-area-inset-top,0px) + 116px);left:50%;transform:translateX(-50%);box-sizing:border-box;width:max-content;max-width:calc(100vw - 24px);padding:16px;border:2px solid #a4cbb6;border-radius:10px;background:#153d30;color:white;font:16px/1.5 sans-serif;text-align:center;touch-action:manipulation;';
      ['pointerdown', 'pointerup', 'touchstart', 'touchend', 'mousedown', 'mouseup', 'click'].forEach(function (event) {
        panel.addEventListener(event, function (e) { e.stopPropagation(); });
      });
      document.body.appendChild(panel);
    }
    if (panel) panel.style.display = visible ? 'block' : 'none';
  },
  VirtualHouse_HasTouch: function () {
    return (navigator.maxTouchPoints > 0 || "ontouchstart" in window) ? 1 : 0;
  }
});
