# Implementation Plan: MCP Persistent Service

**Branch**: `001-mcp-persistent-service` | **Date**: 2026-03-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-mcp-persistent-service/spec.md`

## Summary

Transform the MCP server from a stdio-based process into a persistent HTTP/SSE service within the Aspire AppHost. Switch transport to `ModelContextProtocol.AspNetCore`, add a new `StreamCommands` audit RPC to the PhysicsServer for full message visibility, reference PhysicsClient library for convenience tools (presets, generators, steering), and extend the MCP tool surface to ~35 tools covering all 12 command types plus high-level operations.

## Technical Context

**Language/Version**: F# on .NET 10.0 (MCP server, PhysicsServer), C# on .NET 10.0 (AppHost, contracts)
**Primary Dependencies**: ModelContextProtocol.AspNetCore 1.1.*, Grpc.Net.Client 2.*, Google.Protobuf 3.*, PhysicsClient (project ref)
**Storage**: N/A (in-memory state cache and bounded command log)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x (integration tests)
**Target Platform**: Linux server (container with GPU passthrough)
**Project Type**: Microservice (MCP server) + gRPC service modifications (PhysicsServer)
**Performance Goals**: State staleness < 2 seconds, concurrent MCP client support
**Constraints**: Must use gRPC for server communication, MCP protocol for client communication
**Scale/Scope**: Single AppHost deployment, N concurrent AI assistant connections

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | MCP communicates with PhysicsServer via gRPC only. No shared mutable state. |
| II. Contract-First | PASS | Proto changes defined before implementation (see contracts/proto-changes.md). |
| III. Shared Nothing | PASS | PhysicsClient is a library, not a service. Library references are permitted. AppHost already references PhysicsClient (precedent). |
| IV. Spec-First | PASS | Feature spec complete with clarifications. |
| V. Compiler-Enforced Contracts | PASS | All new public F# modules will have .fsi signature files. Plan identifies: PresetTools.fsi, GeneratorTools.fsi, SteeringTools.fsi, AuditTools.fsi, updated MessageRouter.fsi, updated GrpcConnection.fsi. |
| VI. Test Evidence | PASS | Integration tests planned via Aspire testing builder. Unit tests for new tool modules. |
| VII. Observability | PASS | MCP server will use ServiceDefaults for health checks and structured logging. |

**Post-Phase 1 Re-check**: All gates still pass. No new projects introduced. Proto contract is additive (no breaking changes). PhysicsClient reference is library-level, not service-to-service.

## Project Structure

### Documentation (this feature)

```text
specs/001-mcp-persistent-service/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: transport, audit stream, library reference decisions
├── data-model.md        # Phase 1: entity definitions and state transitions
├── quickstart.md        # Phase 1: build/run/connect guide
├── contracts/
│   └── proto-changes.md # Phase 1: CommandEvent message, StreamCommands RPC
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── PhysicsSandbox.Shared.Contracts/
│   └── Protos/
│       └── physics_hub.proto          # MODIFY: add CommandEvent, StreamCommands RPC
├── PhysicsServer/
│   ├── Hub/
│   │   ├── MessageRouter.fs           # MODIFY: add CommandSubscribers, publishCommandEvent
│   │   ├── MessageRouter.fsi          # MODIFY: add subscribeCommands, unsubscribeCommands
│   │   └── StateCache.fs              # UNCHANGED
│   └── Services/
│       └── PhysicsHubService.fs       # MODIFY: implement StreamCommands, hook audit into submitCommand/submitViewCommand
├── PhysicsSandbox.Mcp/
│   ├── Program.fs                     # MODIFY: switch to WebApplication + HTTP/SSE transport
│   ├── GrpcConnection.fs              # MODIFY: add view command stream, audit stream, command log
│   ├── GrpcConnection.fsi            # MODIFY: add new members
│   ├── SimulationTools.fs             # UNCHANGED (already covers all 9 simulation commands)
│   ├── ViewTools.fs                   # UNCHANGED (already covers all 3 view commands)
│   ├── QueryTools.fs                  # MODIFY: add audit query capabilities
│   ├── ClientAdapter.fs               # NEW: adapter bridging GrpcConnection with PhysicsClient modules
│   ├── ClientAdapter.fsi              # NEW: signature file
│   ├── PresetTools.fs                 # NEW: body preset tools (delegates to PhysicsClient.Presets)
│   ├── PresetTools.fsi                # NEW: signature file
│   ├── GeneratorTools.fs              # NEW: scene generator tools (delegates to PhysicsClient.Generators)
│   ├── GeneratorTools.fsi             # NEW: signature file
│   ├── SteeringTools.fs               # NEW: steering tools (delegates to PhysicsClient.Steering)
│   ├── SteeringTools.fsi              # NEW: signature file
│   ├── AuditTools.fs                  # NEW: command audit query tools
│   ├── AuditTools.fsi                 # NEW: signature file
│   └── PhysicsSandbox.Mcp.fsproj      # MODIFY: add ModelContextProtocol.AspNetCore, PhysicsClient ref
├── PhysicsSandbox.AppHost/
│   └── AppHost.cs                     # MODIFY: update MCP resource for HTTP endpoint
└── PhysicsClient/                     # UNCHANGED (referenced as library)

tests/
├── PhysicsServer.Tests/               # MODIFY: add MessageRouter audit subscriber tests
├── PhysicsSandbox.Integration.Tests/  # MODIFY: add MCP HTTP transport tests, audit stream tests
└── PhysicsSandbox.Mcp.Tests/          # NEW (if needed): unit tests for new tool modules
```

**Structure Decision**: No new projects. Changes span 4 existing projects (Contracts, Server, MCP, AppHost) plus their test projects. PhysicsClient is referenced but not modified.

## Complexity Tracking

No constitution violations to justify. All changes fit within existing project boundaries.
