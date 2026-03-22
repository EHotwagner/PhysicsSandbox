Kill all PhysicsSandbox-related processes (AppHost, services, Aspire, DCP).

Run the following bash command:

```bash
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
    echo "Stopping processes..."
    for pat in "${PATTERNS[@]}"; do
        pkill -f "$pat" 2>/dev/null || true
    done
    sleep 2
    for pat in "${PATTERNS[@]}"; do
        pkill -9 -f "$pat" 2>/dev/null || true
    done
    echo "All project processes stopped."
else
    echo "No project processes found."
fi
```

Report what was found and stopped.
