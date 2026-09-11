#!/usr/bin/env bash
# Linux: apt + psql + deploy/setup-postgres.sql (same as hephaestus install-postgres.sh).
# Re-run wipes the sites database (tracking data).
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=wait.sh
. "${SCRIPT_DIR}/wait.sh"

SQL="${SCRIPT_DIR}/setup-postgres.sql"
if [ ! -f "$SQL" ]; then
  echo "Missing: $SQL" >&2
  exit 1
fi

echo "[sites-postgres] postgresql"
ensure_pkg postgresql
ensure_pkg postgresql-client

if command -v systemctl >/dev/null 2>&1; then
  systemctl enable postgresql >/dev/null 2>&1 || true
  systemctl start postgresql >/dev/null 2>&1 || true
fi

run_as_postgres() {
  if [ "${EUID:-0}" -eq 0 ]; then
    if command -v runuser >/dev/null 2>&1; then
      runuser -u postgres -- "$@"
    else
      sudo -u postgres "$@"
    fi
  else
    sudo -n -u postgres "$@"
  fi
}

echo "[sites-postgres] waiting for server"
for _ in $(seq 1 30); do
  if run_as_postgres pg_isready -q; then
    break
  fi
  sleep 1
done

if ! run_as_postgres pg_isready -q; then
  echo "[sites-postgres] ERROR: PostgreSQL did not become ready." >&2
  exit 1
fi

echo "[sites-postgres] applying setup-postgres.sql"
run_as_postgres psql -d postgres -v ON_ERROR_STOP=1 < "$SQL"
echo "[sites-postgres] done (database sites, role tss)"
