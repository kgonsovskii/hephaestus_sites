Static files for proxied sites (`profiles/{profile}/wwwroot/`).

Each site has a folder named after its publish domain:
  tube-18.xyz/
  tubepleasure.xyz/
  veryoldgames.xyz/

Files are auto-served at matching URL paths on site reload/invalidate:
  profiles/default/wwwroot/tube-18.xyz/player/kt_player.js  ->  GET /player/kt_player.js
  profiles/default/wwwroot/tube-18.xyz/videoscript.js       ->  GET /videoscript.js

Optional localAssets entries in sites.json are aliases only (extra URL -> same file).
Internal files like *.patch.js and README.txt are not published.

Local files override upstream when both exist.
