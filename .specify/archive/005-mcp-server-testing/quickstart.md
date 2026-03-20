# Quickstart: MCP Server and Integration Testing

**Date**: 2026-03-20 | **Feature**: 005-mcp-server-testing

## Prerequisites

- .NET 10.0 SDK
- Aspire workload installed (`dotnet workload install aspire`)
- PhysicsSandbox solution builds: `dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true`

## Bug Fixes (Verify)

### 1. Simulation SSL Fix

After applying the fix to `SimulationClient.fs`:

```bash
# Start the stack
dotnet run --project src/PhysicsSandbox.AppHost

# In the Aspire dashboard (https://localhost:15888), verify:
# - simulation resource is Running (not errored)
# - server console logs show "Simulation connected"
```

### 2. Viewer DISPLAY Fix

After applying the fix to `AppHost.cs`:

```bash
# On a system with X11/Wayland:
echo $DISPLAY  # should show :0 or similar

# Start the stack
dotnet run --project src/PhysicsSandbox.AppHost

# The viewer window should appear (or at least not crash due to missing DISPLAY)
```

## MCP Server

### Build

```bash
dotnet build src/PhysicsSandbox.Mcp
```

### Run Standalone

```bash
# Start Aspire stack first
dotnet run --project src/PhysicsSandbox.AppHost &

# Run MCP server (default: https://localhost:7180)
dotnet run --project src/PhysicsSandbox.Mcp

# Or with custom address
dotnet run --project src/PhysicsSandbox.Mcp -- https://localhost:7180
```

### Configure in Claude Code

Add to `.claude/settings.local.json`:

```json
{
  "mcpServers": {
    "physics-sandbox": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/PhysicsSandbox.Mcp"]
    }
  }
}
```

### Verify Tools

Once configured, the MCP client should discover ~15 tools:
- `add_body`, `apply_force`, `apply_impulse`, `apply_torque`, `set_gravity`
- `step`, `play`, `pause`, `remove_body`, `clear_forces`
- `set_camera`, `set_zoom`, `toggle_wireframe`
- `get_state`, `get_status`

### Quick Test

```
> add_body shape=sphere radius=0.5 y=10 mass=1
Success: Command accepted

> step
Success: Command accepted

> get_state
Simulation State (cached 0.1s ago)
Time: 0.016s | Running: false | Bodies: 1
...
```

## Integration Tests

```bash
# Run all tests (headless, no GPU needed)
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run only integration tests
dotnet test tests/PhysicsSandbox.Integration.Tests

# Run with verbose output
dotnet test tests/PhysicsSandbox.Integration.Tests -v normal
```

Expected: All existing tests (118 unit + 5 integration) continue to pass, plus ~15+ new integration tests.
