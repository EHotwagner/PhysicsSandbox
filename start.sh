#!/usr/bin/env bash
set -euo pipefail

# Start PhysicsSandbox via Aspire AppHost
# Usage: ./start.sh [--https]
#   --https  Use HTTPS profile (TLS, self-signed dev certs)
#   default  Uses HTTP profile (required for Aspire Dashboard MCP with Claude Code)
#
# NOTE: Start this BEFORE Claude Code. Claude Code does not retry failed MCP
# connections (github.com/anthropics/claude-code/issues/31198). Use:
#   ./start.sh && MCP_TIMEOUT=10000 claude

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/src/PhysicsSandbox.AppHost"

PROFILE="http"
if [[ "${1:-}" == "--https" ]]; then
    PROFILE="https"
fi

# Kill any existing AppHost and all related processes to avoid duplicate stacks
"$SCRIPT_DIR/kill.sh"
echo ""

echo "Starting PhysicsSandbox AppHost (profile: $PROFILE)..."
echo "  Dashboard: $(if [[ $PROFILE == "https" ]]; then echo 'https://localhost:8081'; else echo 'http://localhost:8081'; fi)"
echo ""

exec dotnet run --project "$PROJECT" --launch-profile "$PROFILE"
