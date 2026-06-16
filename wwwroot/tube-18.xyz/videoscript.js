(function () {
  "use strict";

  var lastAlertMs = {};

  function alertOnce(key, message) {
    var now = Date.now();
    if (lastAlertMs[key] && now - lastAlertMs[key] < 400) {
      return;
    }
    lastAlertMs[key] = now;
    alert(message);
  }

  function notify(kind, eventName) {
    var name = String(eventName || "").toLowerCase();
    var key = kind + ":" + name;

    if (
      name === "play" ||
      name === "resume" ||
      name.indexOf("playstart") >= 0 ||
      name.indexOf("onplaystart") >= 0 ||
      (kind === "kvs" && name.indexOf("play") >= 0 && name.indexOf("stop") < 0 && name.indexOf("pause") < 0)
    ) {
      alertOnce("play:" + key, "OnPlayStart\n" + window.location.href);
      return;
    }

    if (name === "pause" || name.indexOf("onpause") >= 0 || (kind === "kvs" && name.indexOf("pause") >= 0)) {
      alertOnce("pause:" + key, "OnPause\n" + window.location.href);
      return;
    }

    if (
      name === "finish" ||
      name === "stop" ||
      name === "ended" ||
      name.indexOf("playstop") >= 0 ||
      name.indexOf("onplaystop") >= 0 ||
      (kind === "kvs" && name.indexOf("stop") >= 0)
    ) {
      alertOnce("stop:" + key, "OnPlayStop\n" + window.location.href);
    }
  }

  document.addEventListener("tube18:player", function (event) {
    var detail = event.detail || {};
    notify(detail.kind || "kvs", detail.name);
  });

  function hookExistingPlayer() {
    if (!window.player_obj || window.player_obj.__tube18Hooked) {
      return;
    }

    if (window.__tube18KtPlayerPatched) {
      return;
    }

    var player = window.player_obj;
    if (typeof player.handler === "function") {
      player.__tube18Hooked = true;
      player.handler(function (eventName) {
        notify("kvs", eventName);
      });
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", hookExistingPlayer);
  } else {
    hookExistingPlayer();
  }
})();
