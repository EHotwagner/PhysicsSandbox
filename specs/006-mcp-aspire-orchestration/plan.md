# Implementation Plan: Add MCP Server to Aspire AppHost Orchestration

**Branch**: `006-mcp-aspire-orchestration` | **Date**: 2026-03-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-mcp-aspire-orchestration/spec.md`

## Summary

Add the PhysicsSandbox.Mcp server as a project resource in the Aspire AppHost so it starts, stops, and is monitored alongside the existing services. The MCP server must resolve the PhysicsServer address via Aspire environment variables (`services__server__https__0` / `services__server__http__0`) rather than its current hardcoded default. This follows the identical pattern already used by PhysicsSimulation, PhysicsViewer, and PhysicsClient.

## Technical Context

**Language/Version**: F# on .NET 10.0 (MCP server), C# on .NET 10.0 (AppHost)
**Primary Dependencies**: ModelContextProtocol 1.1.0, Grpc.Net.Client 2.x, Microsoft.Extensions.Hosting 10.x, Aspire.Hosting 13.1.3
**Storage**: N/A
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x
**Target Platform**: Linux (container)
**Project Type**: Distributed application (Aspire-orchestrated microservices)
**Performance Goals**: N/A (no new performance-sensitive paths)
**Constraints**: MCP server uses stdio transport — it is not an HTTP/gRPC server itself, only a gRPC client
**Scale/Scope**: Single project registration + minor Program.fs change

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | PASS | MCP server remains independently deployable; only adds orchestration registration |
| II. Contract-First | PASS | No new contracts needed — MCP server is a gRPC client consuming existing PhysicsHub proto |
| III. Shared Nothing | PASS | Only dependency is Shared.Contracts (already present) |
| IV. Spec-First Delivery | PASS | This spec + plan exist |
| V. Compiler-Enforced Contracts | PASS | No new public modules. GrpcConnection.fsi unchanged. If Program.fs changes signature, .fsi not required (it's the entry point) |
| VI. Test Evidence | PASS | Integration test for MCP resource appearing in Aspire dashboard required |
| VII. Observability | PASS | Aspire dashboard integration is the core deliverable |

**Gate result: PASS — no violations.**

## Project Structure

### Documentation (this feature)

```text
specs/006-mcp-aspire-orchestration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (minimal — no new entities)
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── PhysicsSandbox.AppHost/
│   └── AppHost.cs              # MODIFIED — add MCP project resource
├── PhysicsSandbox.Mcp/
│   └── Program.fs              # MODIFIED — read server address from env vars
tests/
└── PhysicsSandbox.Integration.Tests/
    └── McpOrchestrationTests.cs  # NEW — verify MCP resource starts in Aspire
```

**Structure Decision**: No new projects or directories. Changes are limited to two existing files plus one new test file. The MCP server already exists as a standalone project; this feature integrates it into the orchestration graph.
