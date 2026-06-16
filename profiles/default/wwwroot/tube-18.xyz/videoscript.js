(function () {
  "use strict";

  var videoIntervalSec = $VideoInterval$;
  var timer = null;

  console.log("[tube-18] VideoInterval", videoIntervalSec, "seconds");

  function clearTimer() {
    if (timer !== null) {
      clearTimeout(timer);
      timer = null;
    }
  }

  function showPopup(message) {
    if (typeof window.tube18PopupWindow === "function") {
      window.tube18PopupWindow(message);
    }
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

  function onPlayStart() {
    clearTimer();
    var ms = Number(videoIntervalSec) * 1000;
    if (!(ms > 0)) {
      return;
    }
    timer = setTimeout(function () {
      timer = null;
      showPopup("OnPlayStart (+" + videoIntervalSec + "s)\n" + window.location.href);
    }, ms);
  }

  function onPause() {
    clearTimer();
    showPopup("OnPause\n" + window.location.href);
  }

  document.addEventListener("tube18:player", function (event) {
    var detail = event.detail || {};
    var name = String(detail.name || "").toLowerCase();

    if (isPlayStartEvent(name)) {
      if (!detail.userPlay) {
        return;
      }
      onPlayStart();
      return;
    }

    if (isPauseEvent(name)) {
      onPause();
    }
  });
})();
