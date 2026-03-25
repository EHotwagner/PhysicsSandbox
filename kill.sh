#!/usr/bin/env bash
set -euo pipefail

# Kill all PhysicsSandbox-related processes
# Used by start.sh and integration tests to avoid port conflicts

# Match against actual binary paths to avoid killing shell sessions or build commands
PATTERNS=(
    "PhysicsSandbox.AppHost/bin"
    "PhysicsServer/bin"
    "PhysicsSimulation/bin"
    "PhysicsViewer/bin"
    "PhysicsClient/bin"
    "PhysicsSandbox.Mcp/bin"
    "--project.*PhysicsSandbox.AppHost"
    "--project.*PhysicsServer"
    "--project.*PhysicsSimulation"
    "--project.*PhysicsViewer"
    "--project.*PhysicsClient"
    "--project.*PhysicsSandbox.Mcp"
    "Aspire.Dashboard.dll"
    "tools/dcp "
    "tools/ext/dcpctrl"
    "tools/ext/bin/dcpproc"
    "testhost.dll.*PhysicsSandbox"
    "dotnet test PhysicsSandbox"
    "vstest.console.dll.*PhysicsSandbox"
    "MSBuild.dll.*nodemode"
    "aspire agent mcp"
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
