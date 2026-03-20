# PhysicsSandbox Development Guidelines

Last updated: 2026-03-20

## Active Technologies

- F# on .NET 10.0 (services), C# on .NET 10.0 (AppHost, ServiceDefaults)
- .NET Aspire 13.1.3, Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, Grpc.Tools 2.x
- xUnit 2.x, Aspire.Hosting.Testing 10.x

## Project Structure

```text
PhysicsSandbox.slnx
src/
  PhysicsSandbox.AppHost/           # C# Aspire orchestrator
  PhysicsSandbox.ServiceDefaults/   # C# shared health/telemetry
  PhysicsSandbox.Shared.Contracts/  # Proto gRPC contracts
  PhysicsServer/                    # F# server hub (message router)
tests/
  PhysicsServer.Tests/              # F# unit tests
  PhysicsSandbox.Integration.Tests/ # C# Aspire integration tests
```

## Commands

```bash
# Build
dotnet build PhysicsSandbox.slnx

# Run (starts Aspire dashboard + server)
dotnet run --project src/PhysicsSandbox.AppHost

# Test
dotnet test PhysicsSandbox.slnx
```

## Code Style

- F# services: `.fsi` signature files required for all public modules (constitution Principle V)
- C# infrastructure projects: standard conventions, no domain logic
- Proto files: `physics_sandbox` package, `PhysicsSandbox.Shared.Contracts` C# namespace

## Recent Changes

- 001-server-hub: Aspire AppHost, gRPC contracts (PhysicsHub + SimulationLink), PhysicsServer hub with state caching and single-simulation enforcement, ServiceDefaults, 13 tests

## Known Issues & Gotchas

### gRPC HTTP/2 Configuration
F# service projects need `Grpc.AspNetCore.Server` (not `Grpc.AspNetCore`) to avoid proto compilation errors in non-C# projects. AppHost must set `ASPNETCORE_KESTREL__ENDPOINTDEFAULTS__PROTOCOLS=Http1AndHttp2` for gRPC over plain HTTP.

### Integration Test SSL
Aspire integration tests connecting via HTTPS need `RemoteCertificateValidationCallback = (_, _, _, _) => true` on the `SocketsHttpHandler` to accept dev certificates.

### Solution Format
Solution file is `.slnx` (XML-based, .NET 10 default), not `.sln`.

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
