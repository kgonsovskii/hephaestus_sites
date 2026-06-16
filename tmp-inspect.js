const http = require("http");
const url = "http://localhost:5080/video/6685/karolaysexy-s-timid-stepsister-gets-a-kitchen-tear-up-in-super-hot-homemade-act-while-stepbro-is-away/";

const events = [];
function fetchText(u) {
  return new Promise((resolve, reject) => {
    http.get(u, (res) => {
      let d = "";
      res.on("data", (c) => (d += c));
      res.on("end", () => resolve({ status: res.statusCode, headers: res.headers, body: d }));
    }).on("error", reject);
  });
}

(async () => {
  const page = await fetchText(url);
  const vs = await fetchText("http://localhost:5080/videoscript.js");
  const kt = await fetchText("http://localhost:5080/player/kt_player.js?v=15.4.2");
  const checks = {
    pageStatus: page.status,
    pageBytes: page.body.length,
    hasVideoscriptTag: page.body.includes("/videoscript.js"),
    videoscriptPosition: page.body.indexOf("/videoscript.js"),
    hasKtPlayerLocal: page.body.includes("/player/kt_player.js"),
    ktPlayerInitLine: (page.body.match(/player_obj.*kt_player/) || [null])[0],
    hasAdvPreVast: page.body.includes("adv_pre_vast"),
    hasPlayerEngagedInKt: kt.body.includes("playerEngaged"),
    hasUserPlayInVs: vs.body.includes("userPlay"),
    ktPlayerBytes: kt.body.length,
    videoscriptBytes: vs.body.length,
    ktPlayerServedFrom: kt.headers["x-hephaestus-local"] || kt.headers["x-local-asset"] || "(no local header)",
  };
  console.log(JSON.stringify(checks, null, 2));
})().catch((e) => { console.error(e); process.exit(1); });
