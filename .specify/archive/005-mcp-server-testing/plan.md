# Implementation Plan: MCP Server and Integration Testing

**Branch**: `005-mcp-server-testing` | **Date**: 2026-03-20 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-mcp-server-testing/spec.md`

## Summary

Three deliverables: (1) Fix the simulation SSL and viewer DISPLAY bugs that block real physics execution, (2) Build an MCP server that exposes all PhysicsHub operations as ~15 fine-grained tools for interactive debugging via AI assistants, (3) Expand the integration test suite from 5 to 20+ tests covering command routing, state streaming, simulation lifecycle, and error conditions.

## Technical Context

**Language/Version**: F# on .NET 10.0 (MCP server, simulation fix), C# on .NET 10.0 (AppHost fix, integration tests)
**Primary Dependencies**: ModelContextProtocol 1.1.0 (MCP SDK), Microsoft.Extensions.Hosting, Grpc.Net.Client 2.x, Google.Protobuf 3.x, Aspire.Hosting.Testing 10.x
**Storage**: N/A (stateless MCP bridge with in-memory state cache)
**Testing**: xUnit 2.x, Aspire DistributedApplicationTestingBuilder (integration), dotnet test
**Target Platform**: Linux (primary), cross-platform .NET 10.0
**Project Type**: Console application (MCP server) + bug fixes + integration tests
**Performance Goals**: `get_state` response < 100ms (cached), command tools < 1s round-trip
**Constraints**: All logging to stderr (stdout is MCP transport), headless CI (no GPU/display for tests)
**Scale/Scope**: ~15 MCP tools, ~2 bug fixes, ~20 integration test scenarios

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | MCP server is standalone, communicates only via gRPC. No shared mutable state. |
| II. Contract-First | PASS | MCP server consumes existing proto contracts. No new gRPC contracts needed. MCP tool schemas documented in contracts/mcp-tools.md. |
| III. Shared Nothing | PASS | MCP server references only PhysicsSandbox.Shared.Contracts (proto-generated types). No cross-service project references. |
| IV. Spec-First Delivery | PASS | This plan follows the spec-kit workflow. Spec and clarifications complete. |
| V. Compiler-Enforced Structural Contracts | PASS | MCP server public modules will have .fsi signature files. |
| VI. Test Evidence | PASS | Integration tests are a primary deliverable. Bug fixes include test coverage. |
| VII. Observability by Default | PASS | MCP server uses Microsoft.Extensions.Logging (to stderr). ServiceDefaults not needed (not an Aspire-managed service). |

**Post-Phase 1 Re-check**: No new violations. MCP server is a new F# project with .fsi files, consuming existing contracts.

## Project Structure

### Documentation (this feature)

```text
specs/005-mcp-server-testing/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: technology decisions
├── data-model.md        # Phase 1: tool definitions and data flow
├── quickstart.md        # Phase 1: build/run/test instructions
├── contracts/
│   └── mcp-tools.md     # Phase 1: MCP tool schemas
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── PhysicsSandbox.Mcp/                 # NEW: F# MCP server project
│   ├── PhysicsSandbox.Mcp.fsproj
│   ├── Program.fs                      # Host builder + MCP server setup
│   ├── GrpcConnection.fs               # gRPC channel + state stream cache
│   ├── GrpcConnection.fsi              # Signature file
│   ├── SimulationTools.fs              # 9 simulation command tools + step/play/pause
│   ├── SimulationTools.fsi             # Signature file
│   ├── ViewTools.fs                    # 3 view command tools
│   ├── ViewTools.fsi                   # Signature file
│   ├── QueryTools.fs                   # get_state, get_status tools
│   └── QueryTools.fsi                  # Signature file
│
├── PhysicsSimulation/
│   └── Client/
│       └── SimulationClient.fs         # MODIFIED: SSL bypass + reconnection loop
│
├── PhysicsSandbox.AppHost/
│   └── AppHost.cs                      # MODIFIED: DISPLAY env for viewer
│
tests/
└── PhysicsSandbox.Integration.Tests/
    ├── ServerHubTests.cs               # EXISTING: 5 tests (unchanged)
    ├── SimulationConnectionTests.cs    # NEW: simulation connect/disconnect/reconnect
    ├── CommandRoutingTests.cs          # NEW: all 9 command types end-to-end
    ├── StateStreamingTests.cs          # NEW: state stream, concurrent subscribers
    └── ErrorConditionTests.cs          # NEW: error scenarios, edge cases
```

**Structure Decision**: New MCP server project follows the same pattern as PhysicsClient — small F# project referencing Shared.Contracts and using gRPC client. Integration tests extend the existing C# test project with new test classes organized by concern.

## Complexity Tracking

No constitution violations to justify.

## Key Implementation Details

### Bug Fix: SimulationClient SSL + Reconnection

Replace `GrpcChannel.ForAddress(serverAddress)` in `SimulationClient.fs` with the `createChannel` pattern from `PhysicsClient/Connection/Session.fs` (SocketsHttpHandler + SSL bypass + HTTP/2 policy). Wrap the existing `run` function's connection logic in a reconnection loop with exponential backoff (1s → 10s max), preserving the BepuPhysics world across reconnections.

### Bug Fix: Viewer DISPLAY

Add `.WithEnvironment("DISPLAY", Environment.GetEnvironmentVariable("DISPLAY") ?? ":0")` to the viewer registration in `AppHost.cs`.

### MCP Server Architecture

The MCP server is a thin bridge: `stdio (MCP) → gRPC (PhysicsHub)`. It uses the `ModelContextProtocol` NuGet package with `Host.CreateApplicationBuilder` and `WithStdioServerTransport()`. Tools are F# types with `[<McpServerToolType>]` and `[<McpServerTool>]` attributes. The `GrpcConnection` module manages the gRPC channel (with SSL bypass) and a background `StreamState` subscription that caches the latest `SimulationState`. All tools receive the `GrpcConnection` via DI.

### Integration Test Strategy

New tests use the existing `DistributedApplicationTestingBuilder` pattern. Tests that need real simulation data (e.g., body physics verification) wait for the simulation to connect, send commands, and poll the state stream with timeouts. Tests are organized into 4 new classes:
- **SimulationConnectionTests**: Verify simulation connects, stays connected, handles reconnection
- **CommandRoutingTests**: All 9 command types reach the simulation and produce state changes
- **StateStreamingTests**: Multiple subscribers, state consistency, late joiner caching
- **ErrorConditionTests**: Commands without simulation, duplicate simulation, invalid parameters
