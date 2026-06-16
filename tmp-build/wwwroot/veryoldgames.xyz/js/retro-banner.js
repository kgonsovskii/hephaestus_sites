// Example banner script — https://veryoldgames.xyz/x/js/retro-banner.js
(function () {
  var el = document.createElement("div");
  el.textContent = "Retro games — static addition from /x/js/retro-banner.js";
  el.style.cssText = "position:fixed;bottom:0;left:0;right:0;padding:6px 12px;background:#1a1a2e;color:#eee;font:13px monospace;z-index:99999";
  document.body.appendChild(el);
})();
