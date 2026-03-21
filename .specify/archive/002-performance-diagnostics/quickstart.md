# Quickstart: Performance Diagnostics & Stress Testing

**Branch**: `002-performance-diagnostics`

## Prerequisites

- .NET 10.0 SDK
- All system packages for Stride3D (openal, freetype2, sdl2, ttf-liberation)
- BepuFSharp 0.1.0 in local NuGet feed (`~/.local/share/nuget-local/`)

## Build & Run

```bash
# Build everything
dotnet build PhysicsSandbox.slnx

# Run (starts all services via Aspire)
dotnet run --project src/PhysicsSandbox.AppHost
```

## Verify New Features

### FPS Display
Launch the system and observe the viewer window. The FPS counter should appear in the top-left overlay alongside the existing simulation time display.

### Batch Commands (via MCP)
Use the `batch_commands` MCP tool to send multiple commands at once:
```
Tool: batch_commands
Commands: [add_body sphere, add_body box, set_gravity 0 -20 0, step]
```

### Restart Simulation (via MCP)
```
Tool: restart_simulation
```
Verify all bodies are removed and simulation time resets to 0.

### Service Metrics (via MCP)
```
Tool: get_metrics
```
Returns message counts and traffic for all services.

### Pipeline Diagnostics (via MCP)
```
Tool: get_diagnostics
```
Returns timing breakdown across simulation, serialization, transfer, and rendering stages.

### Stress Test (via MCP)
```
Tool: start_stress_test
Scenario: body-scaling
Max bodies: 500
```
Then poll with:
```
Tool: get_stress_test_status
Test ID: <returned ID>
```

### MCP vs Scripting Comparison (via MCP)
```
Tool: start_comparison_test
Scenario: add-100-bodies
```

## Test

```bash
# Run all tests (headless)
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
```

## Key Files Changed

| Area | Files |
|------|-------|
| Proto contracts | `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` |
| Server batch/metrics | `src/PhysicsServer/Hub/MessageRouter.fs`, `Services/PhysicsHubService.fs` |
| Simulation reset/static | `src/PhysicsSimulation/World/SimulationWorld.fs` |
| Viewer FPS | `src/PhysicsViewer/Program.fs` |
| MCP new tools | `src/PhysicsSandbox.Mcp/BatchTools.fs`, `MetricsTools.fs`, `DiagnosticsTools.fs`, `StressTestTools.fs` |
| Client library | `src/PhysicsClient/Commands/SimulationCommands.fs` |
