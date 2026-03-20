# Quickstart: Contracts and Server Hub

**Feature**: 001-server-hub | **Date**: 2026-03-20

## Prerequisites

- .NET 9.0 SDK
- Podman (rootless) — for Aspire container resource support
- An IDE or editor with F# and C# support

## Build and Run

```bash
# Clone and navigate to the project
cd PhysicsSandbox

# Restore and build the entire solution
dotnet build PhysicsSandbox.sln

# Run the Aspire AppHost (starts orchestrator + server hub)
dotnet run --project src/PhysicsSandbox.AppHost
```

The Aspire dashboard will be available at `https://localhost:15888`. The server hub should appear as a healthy, running resource.

## Run Tests

```bash
# Unit tests (F# — MessageRouter, StateCache)
dotnet test tests/PhysicsServer.Tests

# Integration tests (C# — full Aspire stack)
dotnet test tests/PhysicsSandbox.Integration.Tests

# All tests
dotnet test PhysicsSandbox.sln
```

## Verify the Server Hub

Once the AppHost is running, you can verify the server hub with a quick gRPC client test:

1. Open the Aspire dashboard at `https://localhost:15888`
2. Confirm the `server` resource shows as **Running** with green health status
3. Check the `/health` and `/alive` endpoints respond (URLs shown in dashboard)

## Project Layout

| Project | Language | Purpose |
|---------|----------|---------|
| `PhysicsSandbox.AppHost` | C# | Aspire orchestrator — starts all services |
| `PhysicsSandbox.ServiceDefaults` | C# | Shared health checks, telemetry, resilience |
| `PhysicsSandbox.Shared.Contracts` | Proto/C# | gRPC contract definitions (proto files) |
| `PhysicsServer` | F# | Server hub — central message router |

## Key Files

- `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto` — all message and service definitions
- `src/PhysicsServer/Services/PhysicsHubService.fs` — gRPC service implementation
- `src/PhysicsServer/Hub/MessageRouter.fs` — command and state routing logic
- `src/PhysicsSandbox.AppHost/Program.cs` — Aspire topology definition

## What's Next

After this feature is merged, the next specs will add:
- **Spec 002**: Simulation service (physics engine + gRPC integration)
- **Spec 003**: Client (REPL + command sending + state display)
- **Spec 004**: Viewer (3D rendering + state/camera streaming)
