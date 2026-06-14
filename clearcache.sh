#!/usr/bin/env bash
# Clears all Sites.Host proxy disk cache (every sourceHost folder).
# Default: /_cache (Linux) or C:/_cache (Git Bash). Override with SITES_CACHE_ROOT.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

resolve_cache_root() {
  if [[ -n "${SITES_CACHE_ROOT:-}" ]]; then
    printf '%s' "$SITES_CACHE_ROOT"
    return
  fi

  local appsettings="$REPO_ROOT/src/Sites.Host/appsettings.json"
  if [[ -f "$appsettings" ]]; then
    local root
    root="$(sed -n 's/.*"RootPath"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$appsettings" | head -n 1)"
    if [[ -n "$root" ]]; then
      printf '%s' "$root"
      return
    fi
  fi

  case "$(uname -s 2>/dev/null || echo unknown)" in
    MINGW*|MSYS*|CYGWIN*|Windows*)
      printf '%s' 'C:/_cache'
      ;;
    *)
      printf '%s' '/_cache'
      ;;
  esac
}

CACHE_ROOT="$(resolve_cache_root)"

if [[ ! -d "$CACHE_ROOT" ]]; then
  echo "Cache directory does not exist (nothing to clear): $CACHE_ROOT"
  exit 0
fi

shopt -s dotglob nullglob
entries=("$CACHE_ROOT"/*)
shopt -u dotglob nullglob

if [[ ${#entries[@]} -eq 0 ]]; then
  echo "Cache already empty: $CACHE_ROOT"
  exit 0
fi

count=0
for entry in "${entries[@]}"; do
  rm -rf "$entry"
  count=$((count + 1))
done

echo "Cleared proxy disk cache: $CACHE_ROOT"
echo "Removed ${count} entr(ies)."
