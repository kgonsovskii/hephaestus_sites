(function () {
  "use strict";

  var attached = false;
  var lastAlertMs = {};

  function alertOnce(key, message) {
    var now = Date.now();
    if (lastAlertMs[key] && now - lastAlertMs[key] < 400) {
      return;
    }
    lastAlertMs[key] = now;
    alert(message);
  }

  function notify(eventName) {
    var name = String(eventName || "").toLowerCase();

    if (
      name === "play" ||
      name === "resume" ||
      name.indexOf("playstart") >= 0 ||
      name.indexOf("onplaystart") >= 0
    ) {
      alertOnce("play", "OnPlayStart\n" + window.location.href);
      return;
    }

    if (name === "pause" || name.indexOf("onpause") >= 0) {
      alertOnce("pause", "OnPause\n" + window.location.href);
      return;
    }

    if (
      name === "finish" ||
      name === "stop" ||
      name === "ended" ||
      name.indexOf("playstop") >= 0 ||
      name.indexOf("onplaystop") >= 0
    ) {
      alertOnce("stop", "OnPlayStop\n" + window.location.href);
    }
  }

  function bindHtml5Video(root) {
    if (!root) {
      return;
    }

    var video = root.querySelector("video");
    if (!video || video.__tube18Videoscript) {
      return;
    }

    video.__tube18Videoscript = true;
    video.addEventListener("play", function () {
      notify("play");
    });
    video.addEventListener("pause", function () {
      notify("pause");
    });
    video.addEventListener("ended", function () {
      notify("ended");
    });
  }

  function bindFlowplayer(fp) {
    if (!fp || typeof fp.bind !== "function" || fp.__tube18Videoscript) {
      return;
    }

    fp.__tube18Videoscript = true;
    fp.bind("play", function () {
      notify("play");
    });
    fp.bind("resume", function () {
      notify("resume");
    });
    fp.bind("pause", function () {
      notify("pause");
    });
    fp.bind("finish", function () {
      notify("finish");
    });
  }

  function bindKvsPlayer(player) {
    if (!player || player.__tube18Videoscript) {
      return !!player;
    }

    if (typeof player.handler === "function") {
      player.handler(function (eventName) {
        notify(eventName);
      });
    }

    if (typeof player.listen === "function") {
      ["play", "pause", "finish", "stop", "resume"].forEach(function (eventName) {
        player.listen(eventName, function () {
          notify(eventName);
        });
      });
    }

    if (typeof player.fpapi === "function") {
      bindFlowplayer(player.fpapi());
    }

    bindHtml5Video(player.container ? player.container() : null);
    bindHtml5Video(document.getElementById("kt_player"));
    bindHtml5Video(document.querySelector(".player"));

    player.__tube18Videoscript = true;
    attached = true;
    return true;
  }

  function tryAttach() {
    if (window.player_obj) {
      return bindKvsPlayer(window.player_obj);
    }

    var root = document.getElementById("kt_player") || document.querySelector(".player");
    bindHtml5Video(root);
    if (root && root.querySelector("video")) {
      attached = true;
      return true;
    }

    return attached;
  }

  function waitForPlayer() {
    if (tryAttach()) {
      return;
    }

    var attempts = 0;
    var timer = setInterval(function () {
      if (tryAttach() || ++attempts > 300) {
        clearInterval(timer);
      }
    }, 100);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", waitForPlayer);
  } else {
    waitForPlayer();
  }
})();
