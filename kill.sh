#!/usr/bin/env bash
set -euo pipefail

# Kill all PhysicsSandbox-related processes
# Used by start.sh and integration tests to avoid port conflicts

PATTERNS=(
    "PhysicsSandbox.AppHost"
    "PhysicsServer"
    "PhysicsSimulation"
    "PhysicsViewer"
    "PhysicsClient"
    "PhysicsSandbox.Mcp"
    "Aspire.Dashboard"
    "tools/dcp "
    "tools/ext/dcpctrl"
    "tools/ext/bin/dcpproc"
)

FOUND=false
for pat in "${PATTERNS[@]}"; do
    if pgrep -f "$pat" > /dev/null 2>&1; then FOUND=true; break; fi
done

if $FOUND; then
    echo "Stopping existing PhysicsSandbox processes..."
    for pat in "${PATTERNS[@]}"; do
        pkill -f "$pat" 2>/dev/null || true
    done
    sleep 2
    # Force kill anything that survived
    for pat in "${PATTERNS[@]}"; do
        pkill -9 -f "$pat" 2>/dev/null || true
    done
    sleep 1
    echo "Existing processes stopped."
else
    echo "No PhysicsSandbox processes found."
fi
