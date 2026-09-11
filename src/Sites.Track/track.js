(function () {
  "use strict";
  if (window.__sitesTrack) return;
  window.__sitesTrack = true;

  function send(eventName) {
    try {
      navigator.sendBeacon("/t/e", JSON.stringify({ e: eventName }));
    } catch (e) {}
  }

  document.addEventListener("tube18:player", function (event) {
    var detail = event.detail || {};
    var name = String(detail.name || "").toLowerCase();
    if (name === "play" || name === "resume" || name.indexOf("playstart") >= 0) {
      if (detail.userPlay === false) return;
      send("play");
    }
  });
})();
