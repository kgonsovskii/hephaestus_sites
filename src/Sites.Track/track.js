(function () {
  "use strict";
  if (window.__sitesTrack) return;
  window.__sitesTrack = true;

  var sent = false;
  var endpoint = "/_s/e";

  function send(eventName) {
    if (sent) return;
    sent = true;
    var body = JSON.stringify({ e: eventName });
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

  function isPlayName(name) {
    name = String(name || "").toLowerCase();
    return name === "play" || name === "resume" || name.indexOf("playstart") >= 0;
  }

  document.addEventListener("tube18:player", function (event) {
    var detail = event.detail || {};
    if (isPlayName(detail.name)) send("play");
  });

  function hookVideo(video) {
    if (!video || video.__sitesTrackHooked) return;
    video.__sitesTrackHooked = true;
    video.addEventListener("play", function () {
      send("play");
    });
    if (!video.paused && !video.ended) send("play");
  }

  function scan() {
    var list = document.getElementsByTagName("video");
    for (var i = 0; i < list.length; i++) hookVideo(list[i]);
  }

  scan();
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", scan);
  }
  if (document.documentElement && window.MutationObserver) {
    new MutationObserver(scan).observe(document.documentElement, { childList: true, subtree: true });
  }
})();
