# Implementation Plan: MCP Data Logging for Analysis

**Branch**: `005-mcp-data-logging` | **Date**: 2026-03-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-mcp-data-logging/spec.md`

## Summary

Add persistent data recording and deferred query capabilities to the PhysicsSandbox MCP server. The MCP server automatically records all incoming simulation state updates and command events to disk using protobuf binary serialization in time-chunked files. Dual retention limits (10-minute time window + 500 MB size cap) with automatic pruning keep storage bounded. New MCP tools enable AI assistants to query recorded data — body trajectories, state snapshots, command events — with paginated results, solving the fundamental problem that MCP tools are too slow to process physics data in real-time.

## Technical Context

**Language/Version**: F# on .NET 10.0
**Primary Dependencies**: Google.Protobuf 3.x (binary serialization), System.Text.Json (session metadata), System.Threading.Channels (async producer-consumer), ModelContextProtocol.AspNetCore 1.1.* (MCP tool registration)
**Storage**: Append-only protobuf binary files at `~/.config/PhysicsSandbox/recordings/`, JSON metadata per session
**Testing**: xUnit 2.x, Aspire.Hosting.Testing 10.x (integration tests)
**Target Platform**: Linux (container with GPU passthrough)
**Project Type**: MCP server extension (new modules within existing `PhysicsSandbox.Mcp` project)
**Performance Goals**: Recording must not add >10% latency to existing MCP tools; queries return within 2-3 seconds for 10-minute windows
**Constraints**: No new NuGet dependencies required; all serialization uses existing Google.Protobuf; storage defaults to 500 MB max
**Scale/Scope**: 60 state updates/second × 10 minutes = ~36,000 snapshots; ~5-20 KB per snapshot depending on body count

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Service Independence | **PASS** | Recording is internal to MCP server. No cross-service state sharing. No new inter-service communication. |
| II. Contract-First | **PASS** | No new gRPC or proto contracts required. Recording consumes existing `SimulationState` and `CommandEvent` proto messages from existing streams. MCP tools follow existing `[<McpServerTool>]` convention. |
| III. Shared Nothing | **PASS** | No new cross-service dependencies. Recording lives entirely within `PhysicsSandbox.Mcp` project. |
| IV. Spec-First | **PASS** | Feature spec completed and clarified before planning. |
| V. Compiler-Enforced Contracts | **PASS** | All new public F# modules will have `.fsi` signature files. Surface area baselines will be added for new modules. |
| VI. Test Evidence | **PASS** | Unit tests for recording engine, chunk writer/reader, session store. Integration tests for end-to-end recording + query via MCP tools. |
| VII. Observability | **PASS** | Recording itself enhances observability. Recording state (active/stopped, storage usage) exposed via MCP tools. Structured logging for recording lifecycle events. |

**Pre-design gate: PASS** — No violations.

**Post-design re-check: PASS** — Design adds no new cross-service dependencies, no new shared state, no new proto contracts. All new modules get `.fsi` files.

## Project Structure

### Documentation (this feature)

```text
specs/005-mcp-data-logging/
├── plan.md              # This file
├── research.md          # Phase 0: storage format, threading, query strategy
├── data-model.md        # Phase 1: entities, on-disk layout, validation rules
├── quickstart.md        # Phase 1: architecture overview, build/test commands
├── contracts/
│   └── mcp-tools.md     # Phase 1: MCP tool parameter/response contracts
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/PhysicsSandbox.Mcp/
├── Recording/
│   ├── Types.fsi                # LogEntry, EntryType, PaginationCursor, wire format constants
│   ├── Types.fs
│   ├── RecordingEngine.fsi      # Recording lifecycle orchestration
│   ├── RecordingEngine.fs
│   ├── ChunkWriter.fsi          # Disk write pipeline (Channel consumer)
│   ├── ChunkWriter.fs
│   ├── ChunkReader.fsi          # Disk read + pagination
│   ├── ChunkReader.fs
│   ├── SessionStore.fsi         # Session metadata CRUD
│   └── SessionStore.fs
├── RecordingTools.fsi           # MCP tools: start/stop/list/delete/status
├── RecordingTools.fs
├── RecordingQueryTools.fsi      # MCP tools: trajectory/snapshots/events/summary
└── RecordingQueryTools.fs

tests/
├── PhysicsSandbox.Mcp.Tests/    # New unit test project (if not existing)
│   ├── ChunkWriterTests.fs
│   ├── ChunkReaderTests.fs
│   ├── SessionStoreTests.fs
│   └── RecordingEngineTests.fs
└── PhysicsSandbox.Integration.Tests/
    └── RecordingIntegrationTests.cs  # End-to-end recording + query
```

**Structure Decision**: All recording functionality lives within the existing `PhysicsSandbox.Mcp` project as a `Recording/` subdirectory. No new projects needed — this is a feature extension of the MCP server, not a new service. Tool classes (`RecordingTools`, `RecordingQueryTools`) sit at the project root alongside existing tool classes (matching the existing pattern of `SimulationTools.fs`, `QueryTools.fs`, etc.).

## Complexity Tracking

No constitution violations to justify. Design stays within a single existing project with no new dependencies.
