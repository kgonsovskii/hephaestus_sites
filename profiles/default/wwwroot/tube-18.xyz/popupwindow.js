(function () {
  "use strict";

  var intervalSec = $VideoInterval$;
  var timer = null;

  function clearTimer() {
    if (timer !== null) {
      clearTimeout(timer);
      timer = null;
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
    var ms = Number(intervalSec) * 1000;
    if (!(ms > 0)) {
      return;
    }
    timer = setTimeout(function () {
      timer = null;
      alert("OnPlayStart (+" + intervalSec + "s)\n" + window.location.href);
    }, ms);
  }

  function onPause() {
    clearTimer();
    alert("OnPause\n" + window.location.href);
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
