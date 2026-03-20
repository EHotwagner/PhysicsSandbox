# PhysicsSandbox — Main Implementation Plan

**Last Updated**: 2026-03-20
**Revision**: Updated with 005-mcp-server-testing archival

## Technical Context

**Language/Version**: F# on .NET 10.0 (services), C# on .NET 10.0 (AppHost, ServiceDefaults)
**Primary Dependencies**: .NET Aspire 13.1.3, Grpc.AspNetCore 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x, BepuFSharp 0.1.0 (local NuGet), Grpc.Net.Client 2.x, Stride.CommunityToolkit* 1.0.0-preview.62 (4 packages), Spectre.Console 0.49.x (client TUI display), ModelContextProtocol 1.1.0 (MCP server)
**Storage**: N/A (in-memory physics world, stateless message routing)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x, Grpc.Net.Client 2.x
**Target Platform**: Linux (rootless Podman for containers)
**Solution Format**: `.slnx` (XML-based, .NET 10 default)

## Project Structure

```text
PhysicsSandbox.slnx

src/
├── PhysicsSandbox.AppHost/              # C# — Aspire orchestrator
│   ├── AppHost.cs                       # Service registration + HTTP/2 config
│   └── Properties/launchSettings.json   # Podman runtime
│
├── PhysicsSandbox.ServiceDefaults/      # C# — shared telemetry, health, resilience
│   └── Extensions.cs                    # AddServiceDefaults() + MapDefaultEndpoints()
│
├── PhysicsSandbox.Shared.Contracts/     # Proto — shared gRPC contracts
│   └── Protos/physics_hub.proto         # PhysicsHub + SimulationLink services
│
├── PhysicsServer/                       # F# — server hub (central message router)
│   ├── Hub/
│   │   ├── StateCache.fsi/.fs           # Latest-state caching for late joiners
│   │   └── MessageRouter.fsi/.fs        # Command/state routing, subscriber mgmt
│   ├── Services/
│   │   ├── PhysicsHubService.fsi/.fs    # Client/viewer-facing gRPC
│   │   └── SimulationLinkService.fsi/.fs # Simulation-facing gRPC
│   └── Program.fs                       # Host setup
│
├── PhysicsSimulation/                   # F# — physics simulation (gRPC client)
│   ├── World/
│   │   ├── SimulationWorld.fsi/.fs      # BepuFSharp world wrapper, body/force management
│   ├── Commands/
│   │   ├── CommandHandler.fsi/.fs       # Command dispatch (9 command types)
│   ├── Client/
│   │   ├── SimulationClient.fsi/.fs     # Bidirectional streaming client, simulation loop
│   └── Program.fs                       # Host setup, Aspire service defaults
│
├── PhysicsViewer/                       # F# — 3D viewer (Stride3D + gRPC client)
│   ├── Rendering/
│   │   ├── SceneManager.fsi/.fs         # SimulationState → Stride entities, wireframe
│   │   └── CameraController.fsi/.fs     # Camera state, input, REPL commands
│   ├── Streaming/
│   │   └── ViewerClient.fsi/.fs         # gRPC streaming client with auto-reconnect
│   └── Program.fs                       # Host + Stride game loop
│
└── PhysicsClient/                       # F# — REPL client library (gRPC client, Spectre.Console)
    ├── Bodies/
    │   ├── IdGenerator.fsi/.fs          # Thread-safe human-readable ID generation
    │   ├── Presets.fsi/.fs              # 7 body presets (marble, bowlingBall, crate, etc.)
    │   └── Generators.fsi/.fs           # Random generators + scene builders
    ├── Connection/
    │   └── Session.fsi/.fs              # gRPC connection, state caching, body registry
    ├── Commands/
    │   ├── SimulationCommands.fsi/.fs   # All simulation command wrappers
    │   └── ViewCommands.fsi/.fs         # Camera, zoom, wireframe wrappers
    ├── Steering/
    │   └── Steering.fsi/.fs             # Push, launch, spin, stop + Direction DU
    ├── Display/
    │   ├── StateDisplay.fsi/.fs         # Spectre.Console tables, panels, staleness
    │   └── LiveWatch.fsi/.fs            # Cancellable live state feed with filters
    ├── Program.fs                       # Aspire entry point
    └── PhysicsClient.fsx                # FSI convenience script
│
└── PhysicsSandbox.Mcp/                 # F# — MCP server (interactive debugging via AI assistants)
    ├── GrpcConnection.fsi/.fs          # gRPC channel + background state stream cache
    ├── SimulationTools.fsi/.fs         # 10 simulation command MCP tools
    ├── ViewTools.fsi/.fs               # 3 view command MCP tools
    ├── QueryTools.fsi/.fs              # get_state, get_status MCP tools
    └── Program.fs                      # Host + stdio MCP transport

tests/
├── PhysicsSandbox.Integration.Tests/    # C# — Aspire end-to-end tests (32 tests)
│   ├── ServerHubTests.cs               # 6 tests (SendCommand, StreamState, SendViewCommand, StreamViewCommands)
│   ├── SimulationConnectionTests.cs    # 7 tests (connection lifecycle, physics verification, 30s stability)
│   ├── CommandRoutingTests.cs          # 10 tests (all 9 command types + ClearForces end-to-end)
│   ├── StateStreamingTests.cs          # 4 tests (concurrent subscribers, late-joiner, view command forwarding)
│   ├── ErrorConditionTests.cs          # 5 tests (no simulation, empty command, rapid stress)
│   └── xunit.runner.json              # Test runner configuration
├── PhysicsServer.Tests/                 # F# — unit tests (13 tests)
│   ├── StateCacheTests.fs
│   ├── MessageRouterTests.fs            # Includes readViewCommand tests
│   └── PublicApiBaseline.txt            # Surface-area baseline
├── PhysicsSimulation.Tests/             # F# — unit tests (37 tests)
│   ├── SimulationWorldTests.fs          # Lifecycle, bodies, forces, gravity, stress
│   ├── CommandHandlerTests.fs           # Command dispatch, edge cases
│   └── SurfaceAreaTests.fs              # Public API baseline verification
├── PhysicsViewer.Tests/                 # F# — unit tests (16 tests)
│   ├── SceneManagerTests.fs             # Shape classification, state accessors
│   ├── CameraControllerTests.fs         # Camera math, command application
│   ├── SurfaceAreaTests.fs              # Public API baseline verification
│   └── PublicApiBaseline.txt            # Surface-area baseline
└── PhysicsClient.Tests/                 # F# — unit tests (52 tests)
    ├── IdGeneratorTests.fs              # Sequential IDs, reset, thread safety
    ├── SessionTests.fs                  # Connection lifecycle
    ├── SimulationCommandsTests.fs       # Proto message construction, Vec3 conversion
    ├── PresetsTests.fs                  # Preset parameters, mass values
    ├── GeneratorsTests.fs               # Scene builder validation, count checks
    ├── SteeringTests.fs                 # Direction-to-Vec3 mapping
    ├── StateDisplayTests.fs             # Vec3 formatting, velocity magnitude, shapes
    └── SurfaceAreaTests.fs              # Public API baseline for all 9 modules
```

## Configuration

- `ASPIRE_CONTAINER_RUNTIME=podman` — set in AppHost launchSettings.json
- `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` — set by AppHost on server resource for gRPC
- `NuGet.config` — local feed at `~/.local/share/nuget-local/` for BepuFSharp package
- Simulation connects to server via Aspire service discovery (`services__server__https__0` env var)
- Viewer connects to server via same Aspire service discovery env vars
- Client connects to server via same Aspire service discovery env vars (fallback: `http://localhost:5000`)
- MCP server connects to PhysicsServer via command-line arg (default: `https://localhost:7180`); not Aspire-managed
- Stride3D uses OpenGL graphics API (`<StrideGraphicsApi>OpenGL</StrideGraphicsApi>`) for container/GPU-passthrough compatibility
- Stride asset compiler disabled by default (`StrideCompilerSkipBuild`); builds without it for CI, enable for live runs with GPU

## Engineering Exceptions

| Exception | Justification |
|-----------|---------------|
| AppHost in C# | No F# Aspire AppHost templates. ~30 lines boilerplate, no domain logic. |
| ServiceDefaults in C# | Standard Aspire template. Extension methods only. |
| Integration tests in C# | Aspire DistributedApplicationTestingBuilder has better C# support. |

## Future Services (Planned)

All four services (Server, Simulation, Viewer, Client) are now implemented. MCP server added as standalone debugging tool (not an Aspire-managed service).

## Known Issues & Gotchas

- gRPC requires HTTP/2; plain HTTP endpoints need `Http1AndHttp2` protocol config. [Source: specs/001-server-hub]
- F# projects must use `Grpc.AspNetCore.Server` (not `Grpc.AspNetCore`) to avoid proto compilation conflicts. [Source: specs/001-server-hub]
- Integration tests must connect via HTTPS endpoint with dev cert validation bypass (`RemoteCertificateValidationCallback = (_, _, _, _) => true`). [Source: specs/001-server-hub]
- Solution file is `.slnx` (XML format) not `.sln` — .NET 10 default. [Source: specs/001-server-hub]
- BepuFSharp must be packed with `-p:NoWarn=NU5104` due to prerelease BepuPhysics2 dependency. [Source: specs/002-physics-simulation]
- Proto `Sphere`/`Box` type names conflict with BepuFSharp shapes in F#; use type aliases (`ProtoSphere`, `ProtoBox`) to disambiguate. [Source: specs/002-physics-simulation]
- Simulation is a Worker service (not Web), acts as gRPC client — no Kestrel config needed on the simulation project itself. [Source: specs/002-physics-simulation]
- Plane bodies are approximated as large static boxes (BepuPhysics2 has no infinite plane). Statics are not tracked in state stream. [Source: specs/002-physics-simulation]
- Stride3D `Vector3` uses `inref<>` operator overloads that don't work with F# `+`/`*`; use `Vector3.Add(&a, &b, &result)` or helper functions. Same for `Vector3.Cross`. [Source: specs/003-3d-viewer]
- Stride's `Game.Run()` blocks the main thread; gRPC streams run on background tasks with `ConcurrentQueue<T>` bridging. [Source: specs/003-3d-viewer]
- Stride's `Add3DCameraController()` conflicts with custom CameraController — do not use both. [Source: specs/003-3d-viewer]
- Viewer needs `openal`, `freetype2`, `sdl2`, `ttf-liberation` system packages and `freeimage.so` symlink on Linux. [Source: specs/003-3d-viewer]
- Viewer uses `DebugTextSystem.Print` for status overlay (no font assets needed). [Source: specs/003-3d-viewer]
- Client library uses `OutputType=Exe` for Aspire orchestration but is also FSI-loadable via `#r` on the compiled DLL. [Source: specs/004-client-repl]
- Client gRPC channels are lazy — `GrpcChannel.ForAddress` succeeds immediately; failures surface on first RPC call. [Source: specs/004-client-repl]
- Client IdGenerator uses ConcurrentDictionary.AddOrUpdate which may invoke the update delegate multiple times under contention — but always produces correct results. Use unique shape keys in tests to avoid cross-test interference. [Source: specs/004-client-repl]
- Spectre.Console Live context with Ctrl+C cancellation: must set `args.Cancel = true` in CancelKeyPress handler to prevent process termination. [Source: specs/004-client-repl]
- MCP server must log to stderr only — stdout is the stdio MCP transport. Use `LogToStandardErrorThreshold = LogLevel.Trace` in host configuration. [Source: specs/005-mcp-server-testing]
- MCP `[<McpServerToolType>]` requires static methods on types — F# types with static members compile correctly for SDK discovery via `WithToolsFromAssembly()`. [Source: specs/005-mcp-server-testing]
- Simulation SSL: must use SocketsHttpHandler with `RemoteCertificateValidationCallback` returning true (same as PhysicsClient and integration tests). Without this, bidirectional stream fails silently on HTTPS. [Source: specs/005-mcp-server-testing]
- Simulation reconnection: exponential backoff (1s → 10s max) preserves BepuPhysics world across stream reconnections. Only exits on CancellationToken cancellation. [Source: specs/005-mcp-server-testing]
- Viewer DISPLAY env: Aspire doesn't propagate DISPLAY automatically; must add `.WithEnvironment("DISPLAY", ...)` in AppHost. Fallback to `:0`. [Source: specs/005-mcp-server-testing]
- Integration tests need `xunit.runner.json` with `"diagnosticMessages": true` for timeout debugging. Tests use 30s+ timeouts for simulation stability verification. [Source: specs/005-mcp-server-testing]
