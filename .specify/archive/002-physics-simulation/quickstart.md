# Quickstart: Physics Simulation Service

**Branch**: `002-physics-simulation` | **Date**: 2026-03-20

## Prerequisites

- .NET 10.0 SDK
- BepuFSharp packed to local NuGet feed
- PhysicsSandbox solution building successfully

## Setup BepuFSharp Package

```bash
# Pack BepuFSharp to local NuGet feed
cd /home/developer/projects/BPEWrapper
dotnet pack BepuFSharp/BepuFSharp.fsproj

# Verify package exists
ls ~/.local/share/nuget-local/BepuFSharp.*.nupkg
```

## Add NuGet Source (if not already configured)

```bash
# In BPSandbox project root, add local feed to NuGet.config
dotnet nuget add source ~/.local/share/nuget-local/ --name local
```

Or add to `NuGet.config`:
```xml
<packageSources>
  <add key="local" value="%HOME%/.local/share/nuget-local/" />
  <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
</packageSources>
```

## Build & Run

```bash
# Build entire solution
dotnet build PhysicsSandbox.slnx

# Run via Aspire (starts server + simulation)
dotnet run --project src/PhysicsSandbox.AppHost

# Run tests
dotnet test PhysicsSandbox.slnx
```

## Verify

1. Aspire dashboard opens at `https://localhost:15888`
2. Both `server` and `simulation` resources show as running
3. Simulation connects to server (check structured logs for connection event)
4. Send a command via integration test or future REPL client

## Project References

```
PhysicsSimulation.fsproj
  ├── PackageReference: BepuFSharp (local NuGet)
  ├── PackageReference: Grpc.Net.Client
  ├── ProjectReference: PhysicsSandbox.Shared.Contracts
  └── ProjectReference: PhysicsSandbox.ServiceDefaults
```

## Key Skills for Implementation

- `/fsgrpc-proto` — extending the proto contract with new commands
- `/fsgrpc-client` — implementing the SimulationLink client (bidirectional streaming)
