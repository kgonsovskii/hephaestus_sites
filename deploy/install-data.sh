#!/usr/bin/env bash
# Clone or reset sibling hephaestus_sites_data (dynamic profiles). Same idea as Hephaestus install-data.
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

export GIT_TERMINAL_PROMPT=0

echo "[sites-install-data] clone=${SITES_CLONE_DIR}"
echo "[sites-install-data] data=${SITES_DATA_DIR}"

if [ -d "${SITES_DATA_DIR}/.git" ]; then
  echo "[sites-install-data] updating existing data repo from origin"
  git -C "${SITES_DATA_DIR}" remote set-url origin "${SITES_DATA_GIT_CLONE_URL}"
  git -C "${SITES_DATA_DIR}" fetch origin
  data_branch="$(git -C "${SITES_DATA_DIR}" rev-parse --abbrev-ref HEAD)"
  if [ "${data_branch}" = "HEAD" ]; then
    git -C "${SITES_DATA_DIR}" reset --hard origin/HEAD
  else
    git -C "${SITES_DATA_DIR}" reset --hard "origin/${data_branch}"
  fi
  git -C "${SITES_DATA_DIR}" clean -fd
  git -C "${SITES_DATA_DIR}" remote set-url origin "${SITES_DATA_GIT_REPO}"
  echo "[sites-install-data] done"
  exit 0
fi

if [ -e "${SITES_DATA_DIR}" ]; then
  echo "[sites-install-data] removing non-git ${SITES_DATA_DIR}"
  rm -rf "${SITES_DATA_DIR}"
fi

mkdir -p "$(dirname "${SITES_DATA_DIR}")"
echo "[sites-install-data] cloning ${SITES_DATA_GIT_REPO}"
git clone "${SITES_DATA_GIT_CLONE_URL}" "${SITES_DATA_DIR}"
git -C "${SITES_DATA_DIR}" remote set-url origin "${SITES_DATA_GIT_REPO}"
echo "[sites-install-data] done"
