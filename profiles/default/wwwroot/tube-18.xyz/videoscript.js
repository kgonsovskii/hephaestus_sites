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

  function notify(detail) {
    var kind = detail.kind || "kvs";
    var name = String(detail.name || "").toLowerCase();
    var key = kind + ":" + name;

    if (isPlayStartEvent(name)) {
      if (!detail.userPlay) {
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
    notify(event.detail || {});
  });
})();
