(function () {
  "use strict";
  if (window.__sitesTrack) return;
  window.__sitesTrack = true;

  var sent = false;
  var endpoint = "/_s/e";

  function isVideoPage() {
    var path = location.pathname || "/";
    return path === "/video" || path.indexOf("/video/") === 0;
  }

  function send(eventName) {
    if (sent || !isVideoPage()) return;
    sent = true;
    var body = JSON.stringify({ e: eventName, p: location.pathname || "/" });
    try {
      if (navigator.sendBeacon && navigator.sendBeacon(endpoint, body)) return;
    } catch (e) {}
    try {
      fetch(endpoint, {
        method: "POST",
        body: body,
        keepalive: true,
        credentials: "same-origin",
        headers: { "Content-Type": "text/plain" }
      });
    } catch (e2) {}
  }

  window.__sitesTrackPlay = function () {
    send("play");
  };

  function isPlayName(name) {
    name = String(name || "").toLowerCase();
    return name === "play" || name === "resume" || name.indexOf("playstart") >= 0;
  }

  document.addEventListener("tube18:player", function (event) {
    var detail = event.detail || {};
    if (!detail.userPlay) return;
    if (!isPlayName(detail.name)) return;
    send("play");
  });
})();
