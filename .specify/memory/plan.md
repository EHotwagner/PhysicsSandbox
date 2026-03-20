# PhysicsSandbox — Main Implementation Plan

**Last Updated**: 2026-03-20
**Revision**: Bootstrapped from first feature archival (001-server-hub)

## Technical Context

**Language/Version**: F# on .NET 10.0 (services), C# on .NET 10.0 (AppHost, ServiceDefaults)
**Primary Dependencies**: .NET Aspire 13.1.3, Grpc.AspNetCore 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x
**Storage**: N/A (stateless message routing)
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
└── PhysicsServer/                       # F# — server hub (central message router)
    ├── Hub/
    │   ├── StateCache.fsi/.fs           # Latest-state caching for late joiners
    │   └── MessageRouter.fsi/.fs        # Command/state routing, subscriber mgmt
    ├── Services/
    │   ├── PhysicsHubService.fsi/.fs    # Client/viewer-facing gRPC
    │   └── SimulationLinkService.fsi/.fs # Simulation-facing gRPC
    └── Program.fs                       # Host setup

tests/
├── PhysicsSandbox.Integration.Tests/    # C# — Aspire end-to-end tests
│   └── ServerHubTests.cs
└── PhysicsServer.Tests/                 # F# — unit tests
    ├── StateCacheTests.fs
    ├── MessageRouterTests.fs
    └── PublicApiBaseline.txt            # Surface-area baseline
```

## Configuration

- `ASPIRE_CONTAINER_RUNTIME=podman` — set in AppHost launchSettings.json
- `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` — set by AppHost on server resource for gRPC

## Engineering Exceptions

| Exception | Justification |
|-----------|---------------|
| AppHost in C# | No F# Aspire AppHost templates. ~30 lines boilerplate, no domain logic. |
| ServiceDefaults in C# | Standard Aspire template. Extension methods only. |
| Integration tests in C# | Aspire DistributedApplicationTestingBuilder has better C# support. |

## Future Services (Planned)

- **Spec 002**: Simulation (physics engine + gRPC integration)
- **Spec 003**: Client (REPL + command sending + state display)
- **Spec 004**: Viewer (3D rendering + state/camera streaming)

## Known Issues & Gotchas

- gRPC requires HTTP/2; plain HTTP endpoints need `Http1AndHttp2` protocol config. [Source: specs/001-server-hub]
- F# projects must use `Grpc.AspNetCore.Server` (not `Grpc.AspNetCore`) to avoid proto compilation conflicts. [Source: specs/001-server-hub]
- Integration tests must connect via HTTPS endpoint with dev cert validation bypass (`RemoteCertificateValidationCallback = (_, _, _, _) => true`). [Source: specs/001-server-hub]
- Solution file is `.slnx` (XML format) not `.sln` — .NET 10 default. [Source: specs/001-server-hub]
