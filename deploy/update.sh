#!/usr/bin/env bash
# Self-update on an already-deployed Linux host: git sync, then local publish + systemd restart.
# Scheduled from CP via systemd-run (survives sites-host stop). Manual: ./deploy/update.sh [profile]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=wait.sh
. "${SCRIPT_DIR}/wait.sh"
# shellcheck source=crypt-git-pat.sh
. "${SCRIPT_DIR}/crypt-git-pat.sh"

export SITES_PROFILE="${1:-default}"
export SITES_CLONE_DIR="${SITES_CLONE_DIR:-$(dirname "$SCRIPT_DIR")}"
export SITES_PUBLISH_DIR="${SITES_PUBLISH_DIR:-${SITES_CLONE_DIR}/release}"
export SITES_SERVICE_NAME="${SITES_SERVICE_NAME:-sites-host}"
export SITES_GIT_REPO="${SITES_GIT_REPO:-https://github.com/kgonsovskii/hephaestus_sites.git}"
export SITES_GIT_CLONE_URL="${SITES_GIT_CLONE_URL:-${SITES_GIT_REPO}}"
if [ -f "${SCRIPT_DIR}/git-pat.enc" ]; then
  PAT="$(read_sites_git_pat_from_encrypted_file)"
  repo="${SITES_GIT_REPO#https://}"
  export SITES_GIT_CLONE_URL="https://x-access-token:${PAT}@${repo}"
fi
export SITES_RUNTIME_IDENTIFIER="${SITES_RUNTIME_IDENTIFIER:-linux-x64}"

LOG="${SITES_UPDATE_LOG:-/var/log/sites-update.log}"
mkdir -p "$(dirname "$LOG")"
touch "$LOG"
exec > >(tee -a "$LOG") 2>&1 0</dev/null

echo "$(date -Is) [sites-update] start profile=${SITES_PROFILE} clone=${SITES_CLONE_DIR}"

echo "$(date -Is) [sites-update] stopping ${SITES_SERVICE_NAME}"
systemctl stop "${SITES_SERVICE_NAME}" 2>/dev/null || true
sleep 1

if [ -d "${SITES_CLONE_DIR}/.git" ]; then
  echo "$(date -Is) [sites-update] git fetch + reset"
  git -C "${SITES_CLONE_DIR}" remote set-url origin "${SITES_GIT_CLONE_URL}" 2>/dev/null || true
  git -C "${SITES_CLONE_DIR}" fetch origin
  branch="$(git -C "${SITES_CLONE_DIR}" rev-parse --abbrev-ref HEAD)"
  if [ "$branch" = "HEAD" ]; then
    git -C "${SITES_CLONE_DIR}" reset --hard origin/HEAD
  else
    git -C "${SITES_CLONE_DIR}" reset --hard "origin/${branch}"
  fi
  git -C "${SITES_CLONE_DIR}" clean -fd
else
  echo "$(date -Is) [sites-update] fresh clone"
  rm -rf "${SITES_CLONE_DIR}"
  git clone --depth 1 "${SITES_GIT_CLONE_URL}" "${SITES_CLONE_DIR}"
fi

echo "$(date -Is) [sites-update] local install (publish + restart)"
bash "${SCRIPT_DIR}/install-local.sh"

echo "$(date -Is) [sites-update] done"
