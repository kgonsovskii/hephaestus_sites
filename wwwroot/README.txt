Static additions for proxied sites (repo-root `wwwroot/`).

URL path              -> file on disk
/x/                   -> wwwroot/{domain}/index.html
/x/js/app.js          -> wwwroot/{domain}/js/app.js

Domain folders (our publish domains):
  tube-18.xyz/
  tubepleasure.xyz/
  veryoldgames.xyz/

Local asset overrides (sites.json localAssets) also live under wwwroot/{domain}/.

Reference in HTML/JS as /x/your-file.js — resolved against the current site's domain folder.
