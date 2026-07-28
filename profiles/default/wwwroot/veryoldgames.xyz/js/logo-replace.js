(function () {
  function replaceLogo() {
    var link = document.querySelector(".header .logo a");
    if (!link || link.querySelector(".vog-logo")) {
      return;
    }

    var img = document.createElement("img");
    img.src = "/img/layout/logo.png";
    img.alt = "Very Old Games";
    img.className = "vog-logo";
    img.width = 238;
    img.height = 56;
    link.insertBefore(img, link.firstChild);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", replaceLogo);
  } else {
    replaceLogo();
  }
})();
