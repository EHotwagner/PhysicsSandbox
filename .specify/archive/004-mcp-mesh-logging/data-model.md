# Data Model: MCP Mesh Fetch Logging

**Branch**: `004-mcp-mesh-logging` | **Date**: 2026-03-23

## Entities

### MeshFetchEvent (Log Entry)

A recorded observation of a single FetchMeshes RPC handled by the server.

| Field | Type | Description |
|-------|------|-------------|
| timestamp | DateTimeOffset | When the FetchMeshes RPC was handled |
| requested_ids | string list | Mesh IDs requested by the subscriber |
| hits | int | Number of mesh IDs found in server cache |
| misses | int | Number of mesh IDs not found |
| missed_ids | string list | Subset of requested_ids that were not found |

**Lifecycle**: Created once per FetchMeshes RPC call. Immutable after creation. Persisted to chunk files. Subject to the same time/size-based pruning as all recording entries.

### EntryType Extension

| Value | Name | Payload |
|-------|------|---------|
| 0 | StateSnapshot | SimulationState proto bytes |
| 1 | CommandEvent | CommandEvent proto bytes |
| 2 | MeshDefinition | MeshGeometry proto bytes |
| **3** | **MeshFetchEvent** | **MeshFetchLog proto bytes** |

### MeshFetchLog (Proto Message — internal to recording)

Wire format for serializing MeshFetchEvent to chunk files.

| Field | Proto Type | Field Number |
|-------|-----------|-------------|
| requested_ids | repeated string | 1 |
| hits | int32 | 2 |
| misses | int32 | 3 |
| missed_ids | repeated string | 4 |

## Relationships

```text
RecordingSession
  └── ChunkFile (1-minute rotation)
       └── LogEntry (binary, sequential)
            ├── StateSnapshot (type=0)
            ├── CommandEvent (type=1)
            ├── MeshDefinition (type=2)
            └── MeshFetchEvent (type=3)  ← NEW
```
