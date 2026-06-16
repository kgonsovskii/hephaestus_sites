(function () {
  "use strict";

  var videoIntervalSec = $VideoInterval$;
  var timer = null;

  function clearTimer() {
    if (timer !== null) {
      clearTimeout(timer);
      timer = null;
    }
  }

  function resetPopupCycle() {
    window.tube18PopupShown = false;
    if (typeof window.tube18PopupDismiss === "function") {
      window.tube18PopupDismiss();
    }
  }

  function showPopup() {
    if (typeof window.tube18PopupWindow === "function") {
      window.tube18PopupWindow();
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
    resetPopupCycle();
    clearTimer();
    var ms = Number(videoIntervalSec) * 1000;
    if (!(ms > 0)) {
      return;
    }

    timer = setTimeout(function () {
      timer = null;
      showPopup();
    }, ms);
  }

  function onPause() {
    clearTimer();
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
