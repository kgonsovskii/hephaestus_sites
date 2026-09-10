#!/usr/bin/env bash
# Clone sibling hephaestus_sites_data (dynamic profiles). Same idea as Hephaestus install-data.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SITES_CLONE_DIR="${SITES_CLONE_DIR:-$(dirname "$SCRIPT_DIR")}"
SITES_DATA_DIR="${SITES_DATA_DIR:-$(dirname "$SITES_CLONE_DIR")/hephaestus_sites_data}"
SITES_DATA_GIT_REPO="${SITES_DATA_GIT_REPO:-https://github.com/kgonsovskii/hephaestus_sites_data.git}"

# shellcheck source=crypt-git-pat.sh
. "${SCRIPT_DIR}/crypt-git-pat.sh"

if [ -f "${SCRIPT_DIR}/git-pat-data.enc" ]; then
  PAT="$(read_sites_data_git_pat_from_encrypted_file)"
  repo="${SITES_DATA_GIT_REPO#https://}"
  SITES_DATA_GIT_CLONE_URL="${SITES_DATA_GIT_CLONE_URL:-https://x-access-token:${PAT}@${repo}}"
else
  SITES_DATA_GIT_CLONE_URL="${SITES_DATA_GIT_CLONE_URL:-${SITES_DATA_GIT_REPO}}"
fi

echo "[sites-install-data] clone=${SITES_CLONE_DIR}"
echo "[sites-install-data] data=${SITES_DATA_DIR}"

if [ -d "${SITES_DATA_DIR}/.git" ]; then
  echo "[sites-install-data] existing data repo — skip clone (runtime git syncs it)"
  exit 0
fi

if [ -e "${SITES_DATA_DIR}" ]; then
  echo "[sites-install-data] removing non-git ${SITES_DATA_DIR}"
  rm -rf "${SITES_DATA_DIR}"
fi

mkdir -p "$(dirname "${SITES_DATA_DIR}")"
echo "[sites-install-data] cloning ${SITES_DATA_GIT_REPO}"
git clone "${SITES_DATA_GIT_CLONE_URL}" "${SITES_DATA_DIR}"
echo "[sites-install-data] done"
