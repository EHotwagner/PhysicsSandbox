#!/usr/bin/env bash
# Extract the Aspire Dashboard browser token from the running AppHost process.
# Usage: ./dashboard-token.sh          # prints token
#        ./dashboard-token.sh --copy   # copies to clipboard
#        ./dashboard-token.sh --url    # prints full login URL
set -euo pipefail

# Find the AppHost log or process output containing the login URL
TOKEN=""

# Strategy 1: Use aspire ps (Aspire CLI 13.2+) — most reliable
if command -v aspire &>/dev/null; then
    TOKEN=$(aspire ps --format json 2>/dev/null | grep -oP '(?<=login\?t=)[a-f0-9]+' || true)
fi

# Strategy 2: Check /tmp/apphost.log (used by nohup/background starts)
if [[ -z "$TOKEN" ]] && [[ -f /tmp/apphost.log ]]; then
    TOKEN=$(grep -oP '(?<=login\?t=)[a-f0-9]+' /tmp/apphost.log | tail -1)
fi

# Strategy 3: Check the AppHost process environment
if [[ -z "$TOKEN" ]]; then
    APPHOST_PID=$(pgrep -f "PhysicsSandbox.AppHost.dll" | head -1 || true)
    if [[ -n "$APPHOST_PID" ]]; then
        TOKEN=$(cat "/proc/$APPHOST_PID/environ" 2>/dev/null | tr '\0' '\n' | grep -oP '(?<=DOTNET_DASHBOARD_FRONTEND_BROWSERTOKEN=).*' || true)
    fi
fi

if [[ -z "$TOKEN" ]]; then
    echo "Error: Could not find Aspire Dashboard token. Is the AppHost running?" >&2
    echo "  Start with: ./start.sh" >&2
    exit 1
fi

# Determine the dashboard base URL
DASHBOARD_URL="http://localhost:8081"

case "${1:-}" in
    --copy)
        echo -n "$TOKEN" | xclip -selection clipboard 2>/dev/null \
            || echo -n "$TOKEN" | xsel --clipboard 2>/dev/null \
            || echo -n "$TOKEN" | wl-copy 2>/dev/null \
            || { echo "Error: No clipboard tool (xclip/xsel/wl-copy) available" >&2; exit 1; }
        echo "Token copied to clipboard: ${TOKEN:0:8}..."
        ;;
    --url)
        echo "${DASHBOARD_URL}/login?t=${TOKEN}"
        ;;
    *)
        echo "$TOKEN"
        ;;
esac
