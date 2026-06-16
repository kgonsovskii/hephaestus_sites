;(function () {
  "use strict";
  if (window.__tube18KtPlayerPatched) {
    return;
  }
  window.__tube18KtPlayerPatched = true;

  var original = window.kt_player;
  if (typeof original !== "function") {
    return;
  }

  function publish(kind, name, data) {
    try {
      document.dispatchEvent(
        new CustomEvent("tube18:player", {
          detail: { kind: kind, name: String(name), data: data }
        })
      );
    } catch (e) {}
  }

  function bindVideo(video) {
    if (!video || video.__tube18Hooked) {
      return;
    }
    video.__tube18Hooked = true;
    video.addEventListener("play", function (event) {
      if (event && event.isTrusted === false) {
        return;
      }
      publish("video", "play");
    });
    video.addEventListener("pause", function () {
      publish("video", "pause");
    });
    video.addEventListener("ended", function () {
      publish("video", "ended");
    });
  }

  function watchVideo(root) {
    if (!root || root.__tube18VideoWatch) {
      return;
    }
    root.__tube18VideoWatch = true;
    bindVideo(root.querySelector("video"));
    new MutationObserver(function () {
      bindVideo(root.querySelector("video"));
    }).observe(root, { childList: true, subtree: true });
  }

  function hookFlowplayer(player) {
    var attempts = 0;
    var timer = setInterval(function () {
      attempts++;
      var fp = typeof player.fpapi === "function" ? player.fpapi() : null;
      if (fp && typeof fp.bind === "function" && !fp.__tube18Hooked) {
        fp.__tube18Hooked = true;
        fp.bind("play", function () {
          publish("fp", "play");
        });
        fp.bind("resume", function () {
          publish("fp", "resume");
        });
        fp.bind("pause", function () {
          publish("fp", "pause");
        });
        fp.bind("finish", function () {
          publish("fp", "finish");
        });
        clearInterval(timer);
      }
      if (attempts > 600) {
        clearInterval(timer);
      }
    }, 100);
  }

  function hookPlayer(player) {
    if (!player || player.__tube18Hooked) {
      return player;
    }

    player.__tube18Hooked = true;
    window.player_obj = player;

    hookFlowplayer(player);
    watchVideo(typeof player.container === "function" ? player.container() : null);
    watchVideo(document.getElementById("kt_player"));

    return player;
  }

  window.kt_player = function () {
    return hookPlayer(original.apply(this, arguments));
  };
})();
