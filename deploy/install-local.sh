#!/usr/bin/env bash
# Publish Sites.Host and (re)start systemd on this machine. Used by update.sh and install-remote.txt.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SITES_CLONE_DIR="${SITES_CLONE_DIR:-$(dirname "$SCRIPT_DIR")}"
SITES_PUBLISH_DIR="${SITES_PUBLISH_DIR:-${SITES_CLONE_DIR}/release}"
SITES_SERVICE_NAME="${SITES_SERVICE_NAME:-sites-host}"
SITES_RUNTIME_IDENTIFIER="${SITES_RUNTIME_IDENTIFIER:-linux-x64}"

: "${SITES_PROFILE:?SITES_PROFILE is required}"

PROFILE_FILE="$(dirname "${SITES_CLONE_DIR}")/profile.txt"
printf '%s\n' "${SITES_PROFILE}" > "${PROFILE_FILE}"

echo "[sites-install] profile=${SITES_PROFILE} clone=${SITES_CLONE_DIR} publish=${SITES_PUBLISH_DIR}"

echo "[sites-install] dotnet publish -> ${SITES_PUBLISH_DIR}"
dotnet publish "${SITES_CLONE_DIR}/src/Sites.Publish/Sites.Publish.csproj" \
  -c Release \
  /t:PublishSites \
  /p:PublishRuntimeIdentifier="${SITES_RUNTIME_IDENTIFIER}"

if [ ! -f "${SITES_PUBLISH_DIR}/Sites.Host.dll" ]; then
  echo "[sites-install] ERROR: Sites.Host.dll missing after publish." >&2
  exit 1
fi

if [ ! -f "${SITES_PUBLISH_DIR}/Sites.Cp.dll" ]; then
  echo "[sites-install] ERROR: Sites.Cp.dll missing after publish (CP/clone requires it)." >&2
  exit 1
fi

echo "[sites-install] stopping ${SITES_SERVICE_NAME}"
systemctl stop "${SITES_SERVICE_NAME}" 2>/dev/null || true

UNIT_PATH="/etc/systemd/system/${SITES_SERVICE_NAME}.service"
echo "[sites-install] writing ${UNIT_PATH}"
cat > "${UNIT_PATH}" <<EOF
[Unit]
Description=Sites reverse proxy host (${SITES_SERVICE_NAME})
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
WorkingDirectory=${SITES_PUBLISH_DIR}
ExecStart=/usr/bin/dotnet ${SITES_PUBLISH_DIR}/Sites.Host.dll
AmbientCapabilities=CAP_NET_BIND_SERVICE
Restart=on-failure
RestartSec=5
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

echo "[sites-install] restarting ${SITES_SERVICE_NAME}"
systemctl daemon-reload
systemctl enable "${SITES_SERVICE_NAME}"
systemctl restart "${SITES_SERVICE_NAME}"
sleep 2

if ! systemctl is-active --quiet "${SITES_SERVICE_NAME}"; then
  echo "[sites-install] ERROR: ${SITES_SERVICE_NAME} is not active." >&2
  systemctl --no-pager --full status "${SITES_SERVICE_NAME}" >&2 || true
  journalctl -u "${SITES_SERVICE_NAME}" -n 80 --no-pager >&2
  exit 1
fi

systemctl --no-pager --full status "${SITES_SERVICE_NAME}" || true
echo "[sites-install] done"
