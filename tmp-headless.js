const { spawn } = require("child_process");
const chrome = process.argv[1];
const url = process.argv[2];
const args = [
  "--headless=new",
  "--disable-gpu",
  "--no-sandbox",
  "--virtual-time-budget=8000",
  "--run-all-compositor-stages-before-draw",
  "--dump-dom",
  url,
];
const child = spawn(chrome, args, { stdio: ["ignore", "pipe", "pipe"] });
let out = "";
child.stdout.on("data", (d) => (out += d.toString()));
child.stderr.on("data", (d) => (out += d.toString()));
child.on("close", () => {
  const hasFp = /flowplayer|fp-ui|kt_player/.test(out);
  console.log(JSON.stringify({ domBytes: out.length, hasPlayerUi: hasFp, ktPlayerVisible: /id=\"kt_player\"[^>]*style=\"[^\"]*visible/.test(out) }, null, 2));
});
