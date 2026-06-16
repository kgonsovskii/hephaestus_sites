(function () {
  "use strict";

  var lastAlertMs = {};
  var pageReady = document.readyState === "complete";
  var userActive = false;

  function markUserActive() {
    userActive = true;
  }

  ["pointerdown", "keydown", "touchstart"].forEach(function (type) {
    document.addEventListener(type, markUserActive, true);
  });

  window.addEventListener("load", function () {
    pageReady = true;
  });

  function canNotifyPlayStart() {
    return pageReady && userActive;
  }

  function alertOnce(key, message) {
    var now = Date.now();
    if (lastAlertMs[key] && now - lastAlertMs[key] < 400) {
      return;
    }
    lastAlertMs[key] = now;
    alert(message);
  }

  function isPlayStartEvent(name) {
    return (
      name === "play" ||
      name === "resume" ||
      name.indexOf("playstart") >= 0 ||
      name.indexOf("onplaystart") >= 0
    );
  }

  function isPauseEvent(name) {
    return name === "pause" || name.indexOf("onpause") >= 0;
  }

  function isStopEvent(name) {
    return (
      name === "finish" ||
      name === "stop" ||
      name === "ended" ||
      name.indexOf("playstop") >= 0 ||
      name.indexOf("onplaystop") >= 0
    );
  }

  function notify(kind, eventName) {
    var name = String(eventName || "").toLowerCase();
    var key = kind + ":" + name;

    if (isPlayStartEvent(name)) {
      if (!canNotifyPlayStart()) {
        return;
      }
      alertOnce("play:" + key, "OnPlayStart\n" + window.location.href);
      return;
    }

    if (isPauseEvent(name)) {
      alertOnce("pause:" + key, "OnPause\n" + window.location.href);
      return;
    }

    if (isStopEvent(name)) {
      alertOnce("stop:" + key, "OnPlayStop\n" + window.location.href);
    }
  }

  document.addEventListener("tube18:player", function (event) {
    var detail = event.detail || {};
    notify(detail.kind || "kvs", detail.name);
  });
})();
