# Implementation Plan: MCP Mesh Fetch Logging

**Branch**: `004-mcp-mesh-logging` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-mcp-mesh-logging/spec.md`

## Summary

Add FetchMeshes RPC observation to the MCP recording pipeline. When the server handles a FetchMeshes request, the MCP recording engine captures a MeshFetchEvent log entry with the requested mesh IDs, hit/miss counts, and missed IDs. A new MCP query tool enables retrieval of these events from recorded sessions.

## Technical Context

**Language/Version**: F# on .NET 10.0 (PhysicsServer, PhysicsSandbox.Mcp), C# on .NET 10.0 (integration tests)
**Primary Dependencies**: Grpc.AspNetCore.Server 2.x, Google.Protobuf 3.x, ModelContextProtocol.AspNetCore 1.1.*, System.Threading.Channels
**Storage**: Append-only protobuf binary files at `~/.config/PhysicsSandbox/recordings/` (existing recording infrastructure)
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x
**Target Platform**: Linux with GPU (container)
**Project Type**: Distributed simulation system (Aspire-orchestrated microservices)
**Performance Goals**: <1ms overhead on FetchMeshes RPC, non-blocking recording writes
**Constraints**: Must use existing async Channel pipeline. One new proto message (MeshFetchLog) for recording serialization — no changes to existing RPC contracts.
**Scale/Scope**: Mesh fetch events are infrequent (few per second at most). Minimal storage impact.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | Pass | MCP observes server activity via callback — no shared mutable state. |
| II. Contract-First | Pass | One new proto message (MeshFetchLog) for recording serialization only. No RPC contract changes. Existing FetchMeshes RPC unchanged. |
| III. Shared Nothing | Pass | Only shared artifact is the existing proto contracts. MCP observes via callback, not project reference. |
| IV. Spec-First | Pass | This plan + spec precede implementation. |
| V. Compiler-Enforced | Requires | New/modified F# modules need .fsi signature files. |
| VI. Test Evidence | Requires | Unit tests for new LogEntry case, ChunkWriter/Reader handling. |
| VII. Observability | Pass | This feature IS observability — adding recording visibility for mesh fetch channel. |

**Gate result: PASS** — no violations.

## Project Structure

### Source Code Changes

```text
src/
  PhysicsSandbox.Shared.Contracts/
    Protos/physics_hub.proto          # MODIFY: add MeshFetchLog message (recording serialization only)
  PhysicsServer/
    Services/PhysicsHubService.fs     # MODIFY: publish mesh fetch observation via CommandEvent audit stream
  PhysicsSandbox.Mcp/
    Recording/Types.fs/.fsi           # MODIFY: add MeshFetchEvent LogEntry case + EntryType
    Recording/ChunkWriter.fs          # MODIFY: handle MeshFetchEvent serialization
    Recording/ChunkReader.fs          # MODIFY: handle MeshFetchEvent deserialization
    Recording/RecordingEngine.fs/.fsi # MODIFY: detect mesh fetch events in command stream
    MeshFetchQueryTools.fsi/.fs       # NEW: query_mesh_fetches MCP tool
    Program.fs                        # MODIFY: wire mesh fetch detection in OnCommandReceived

tests/
  PhysicsSandbox.Mcp.Tests/
    ChunkWriterTests.fs               # MODIFY: add MeshFetchEvent write/read test
```

**Structure Decision**: No new projects. All changes fit within existing PhysicsServer (callback) and PhysicsSandbox.Mcp (recording + query tool) boundaries.

## Complexity Tracking

No violations to justify. All changes extend existing patterns.
