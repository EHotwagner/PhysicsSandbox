# Quickstart: MCP Data Logging for Analysis

**Branch**: `005-mcp-data-logging` | **Date**: 2026-03-23

## Overview

This feature adds persistent data recording and deferred query capabilities to the PhysicsSandbox MCP server. Recording starts automatically when the MCP server connects to the simulation, capturing all state updates and command events to disk. AI assistants can then query this recorded data through new MCP tools to analyze simulation behavior without real-time constraints.

## Architecture

```text
GrpcConnection (existing)
├── State Stream ──→ RecordingEngine ──→ Channel<LogEntry> ──→ ChunkWriter ──→ disk
├── Command Stream ──→ RecordingEngine ──→ Channel<LogEntry> ──→ ChunkWriter ──→ disk
└── (existing tool serving unchanged)

New MCP Tools
├── Session tools (start/stop/list/delete/status)
└── Query tools (trajectory/snapshots/events/summary) ──→ ChunkReader ──→ disk
```

## New Modules (in PhysicsSandbox.Mcp)

| Module | Responsibility |
|--------|---------------|
| `RecordingEngine` | Orchestrates recording lifecycle, hooks into GrpcConnection streams, manages active session |
| `ChunkWriter` | Consumes from Channel, writes length-prefixed protobuf to chunk files, handles rotation and pruning |
| `ChunkReader` | Reads and deserializes chunk files for queries, supports pagination cursors |
| `SessionStore` | Manages session metadata (session.json), CRUD operations, directory lifecycle |
| `RecordingTools` | MCP tool class for session management (start/stop/list/delete/status) |
| `RecordingQueryTools` | MCP tool class for data queries (trajectory/snapshots/events/summary) |

## Key Patterns

- **Producer-consumer via Channel<T>**: Stream callbacks enqueue LogEntry records without blocking. A background writer task drains the channel to disk.
- **Chunk-per-minute files**: Each chunk is a self-contained binary file. Pruning = delete oldest chunk files. No file rewriting needed.
- **Protobuf binary format**: Reuses existing `Google.Protobuf` serialization. `CalculateSize()` tracks storage budget.
- **Dual-limit pruning**: ChunkWriter checks both time window (10 min default) and size cap (500 MB default) after each chunk rotation.

## Build & Test

```bash
# Build (includes new modules)
dotnet build PhysicsSandbox.slnx

# Run MCP server (recording auto-starts on simulation connection)
dotnet run --project src/PhysicsSandbox.Mcp

# Test
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
```

## Storage Location

```text
~/.config/PhysicsSandbox/recordings/
├── {session-guid}/
│   ├── session.json      # Metadata
│   └── chunk-*.bin       # Binary protobuf data
```

## Dependencies

No new NuGet packages required. Uses existing:
- `Google.Protobuf` — binary serialization of SimulationState and CommandEvent
- `System.Text.Json` — session metadata (follows ViewerSettings pattern)
- `System.Threading.Channels` — built into .NET runtime
