# PhysicsSandbox Development Guidelines

Last updated: 2026-03-21

## Active Technologies
- F# on .NET 10.0 (services, MCP, client), C# on .NET 10.0 (AppHost, ServiceDefaults, Contracts)
- .NET Aspire 13.1.3, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x
- BepuFSharp 0.1.0 (local NuGet, physics engine wrapper), Grpc.Net.Client 2.x
- Stride.CommunityToolkit* 1.0.0-preview.62 (4 packages, 3D viewer)
- Spectre.Console (client library TUI display)
- ModelContextProtocol.AspNetCore 1.1.* (MCP server, persistent HTTP/SSE transport)
- xUnit 2.x, Aspire.Hosting.Testing 10.x
- In-memory storage (physics world, metrics counters, stress test state, command logs)
- F# scripts (.fsx) on .NET 10.0 + PhysicsClient.dll, PhysicsSandbox.Shared.Contracts.dll (proto-generated types) (001-demo-script-modernization)
- F# scripts (.fsx) on .NET 10.0 + PhysicsClient.dll (existing), PhysicsSandbox.Shared.Contracts.dll (existing), Grpc.Net.Client, Google.Protobuf (003-stress-test-demos)
- N/A (in-memory physics simulation) (003-stress-test-demos)
- Python 3.10+ + grpcio, grpcio-tools, protobuf (for gRPC stub generation and communication) (004-python-demo-scripts)
- N/A (stateless scripts communicating with running server) (004-python-demo-scripts)

## Project Structure

```text
PhysicsSandbox.slnx
src/
  PhysicsSandbox.AppHost/           # C# Aspire orchestrator
  PhysicsSandbox.ServiceDefaults/   # C# shared health/telemetry
  PhysicsSandbox.Shared.Contracts/  # Proto gRPC contracts
  PhysicsServer/                    # F# server hub (message router)
  PhysicsSimulation/                # F# physics simulation (gRPC client, BepuFSharp)
  PhysicsViewer/                    # F# 3D viewer (Stride3D + gRPC client)
  PhysicsClient/                    # F# REPL client library (gRPC client, Spectre.Console)
  PhysicsSandbox.Mcp/               # F# MCP server (38 tools, interactive debugging via AI assistants)
tests/
  PhysicsServer.Tests/              # F# unit tests (18 tests)
  PhysicsSimulation.Tests/          # F# unit tests (39 tests)
  PhysicsViewer.Tests/              # F# unit tests (19 tests)
  PhysicsClient.Tests/              # F# unit tests (52 tests)
  PhysicsSandbox.Integration.Tests/ # C# Aspire integration tests (42 tests)
```

## Commands

```bash
# Build
dotnet build PhysicsSandbox.slnx

# Build (headless/CI, skip Stride asset compiler)
dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run (starts Aspire dashboard + server + simulation + viewer + client + mcp)
dotnet run --project src/PhysicsSandbox.AppHost

# Run (with process cleanup, kills existing instances first)
./start.sh          # HTTPS profile
./start.sh --http   # HTTP profile

# Test
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run MCP server (for interactive debugging via AI assistants)
dotnet run --project src/PhysicsSandbox.Mcp
# Or with custom server address:
dotnet run --project src/PhysicsSandbox.Mcp -- https://localhost:7180
```

## Code Style

- F# services: `.fsi` signature files required for all public modules (constitution Principle V)
- C# infrastructure projects: standard conventions, no domain logic
- Proto files: `physics_sandbox` package, `PhysicsSandbox.Shared.Contracts` C# namespace

## Recent Changes
- 004-python-demo-scripts: Added Python 3.10+ + grpcio, grpcio-tools, protobuf (for gRPC stub generation and communication)
- 003-stress-test-demos: Added F# scripts (.fsx) on .NET 10.0 + PhysicsClient.dll (existing), PhysicsSandbox.Shared.Contracts.dll (existing), Grpc.Net.Client, Google.Protobuf
- 001-demo-script-modernization: Demo scripts modernized with batch commands (6 demos use batchAdd) and server-side resetSimulation. Prelude.fsx adds 8 helpers: resetSimulation, makeSphereCmd, makeBoxCmd, makeImpulseCmd, makeTorqueCmd, batchAdd (auto-split at 100), nextId, toVec3. AllDemos/AutoRun/RunAll synced. 37 tasks completed.

## Environment

- Container with GPU passthrough — not headless
- Stride3D viewer and other GPU workloads are expected to run

## Known Issues & Gotchas

### gRPC HTTP/2 Configuration
F# service projects need `Grpc.AspNetCore.Server` (not `Grpc.AspNetCore`) to avoid proto compilation errors in non-C# projects. AppHost must set `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` for gRPC over plain HTTP.

### Integration Test SSL
Aspire integration tests connecting via HTTPS need `RemoteCertificateValidationCallback = (_, _, _, _) => true` on the `SocketsHttpHandler` to accept dev certificates.

### Solution Format
Solution file is `.slnx` (XML-based, .NET 10 default), not `.sln`.

### BepuFSharp NuGet Packaging
Pack BepuFSharp with `-p:NoWarn=NU5104` to suppress prerelease dependency warnings from BepuPhysics2 beta packages. Local NuGet feed at `~/.local/share/nuget-local/`.

### Proto Type Name Conflicts
Proto `Sphere`/`Box` type names conflict with BepuFSharp shapes in F#. Use type aliases (`ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere`) to disambiguate.

### Plane Bodies
Planes are approximated as large static boxes (BepuPhysics2 has no infinite plane). Static bodies are not tracked in the simulation state stream.

### Stride3D Vector3 Interop
Stride's `Vector3` uses `inref<>` operator overloads that don't work with F# `+`/`*`. Use `Vector3.Add(&a, &b, &result)` or helper functions. Same applies to `Vector3.Cross` — must use `byref` calling convention.

### Stride3D Camera Controller Conflict
Do not use Stride's `Add3DCameraController()` alongside a custom CameraController — they fight for input. Use `Add3DCamera()` only and apply camera transforms manually.

### Stride3D Linux Dependencies
Viewer needs `openal`, `freetype2`, `sdl2`, `ttf-liberation` system packages. FreeImage requires `freeimage.so` symlink (`ln -sf /usr/lib/libfreeimage.so /usr/lib/freeimage.so`). GLSL shader compiler binary must be at `linux-x64/glslangValidator.bin`.

### Stride3D Asset Compiler
`StrideCompilerSkipBuild=true` skips asset compilation for CI/headless builds. For live GPU runs, build without this flag (requires fonts + FreeImage). The viewer's `.fsproj` defaults to `false` unless overridden.
