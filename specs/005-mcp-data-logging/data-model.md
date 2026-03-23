# Data Model: MCP Data Logging for Analysis

**Branch**: `005-mcp-data-logging` | **Date**: 2026-03-23

## Entities

### RecordingSession

Represents a single recording capture, mapping to a directory on disk.

| Field | Type | Description |
|-------|------|-------------|
| Id | string (GUID) | Unique session identifier, used as directory name |
| Label | string | User-provided or auto-generated descriptive label |
| Status | enum | `Recording`, `Completed`, `Failed` |
| StartTime | DateTimeOffset | When recording began |
| EndTime | DateTimeOffset option | When recording stopped (None if still active) |
| TimeLimitMinutes | int | Configured time retention limit (default 10) |
| SizeLimitBytes | int64 | Configured size retention limit (default 500 MB) |
| CurrentSizeBytes | int64 | Current total size of all chunk files |
| ChunkCount | int | Number of chunk files in session |
| SnapshotCount | int64 | Total state snapshots recorded |
| EventCount | int64 | Total command events recorded |

**State transitions**: `Recording` → `Completed` (user stops or new session starts) | `Recording` → `Failed` (disk full, I/O error)

**Identity**: `Id` (GUID). One session active at a time (FR-013).

### ChunkFile

Represents a single time-based chunk of recorded data (approximately 1 minute).

| Field | Type | Description |
|-------|------|-------------|
| FileName | string | `chunk-{startTimestamp}.bin` |
| StartTime | DateTimeOffset | Timestamp of first entry in chunk |
| EndTime | DateTimeOffset | Timestamp of last entry in chunk |
| SizeBytes | int64 | File size on disk |
| EntryCount | int | Number of log entries in this chunk |

**Lifecycle**: Created when a new minute boundary is crossed during recording. Deleted during pruning (oldest first). Immutable once closed (new chunk opened for next minute).

### LogEntry

A single recorded item within a chunk file. This is the on-disk binary format (length-prefixed protobuf).

| Field | Type | Description |
|-------|------|-------------|
| Timestamp | int64 | Unix timestamp in milliseconds (high-resolution) |
| EntryType | enum | `StateSnapshot` or `CommandEvent` |
| Payload | bytes | Serialized protobuf message (SimulationState or CommandEvent) |

**Wire format**: `[uint32 totalSize | int64 timestamp | byte entryType | byte[] payload]`

**Relationships**:
- A `RecordingSession` contains 0..N `ChunkFile`s
- A `ChunkFile` contains 1..N `LogEntry`s
- `LogEntry` payload is either a `SimulationState` (from proto) or a `CommandEvent` (from proto)

### PaginationCursor

Opaque cursor for paginated query results.

| Field | Type | Description |
|-------|------|-------------|
| ChunkFileName | string | Which chunk file to resume from |
| ByteOffset | int64 | Byte position within the chunk |

**Encoding**: Base64-encoded JSON. Opaque to callers.

## On-Disk Layout

```text
~/.config/PhysicsSandbox/recordings/
├── {session-id-1}/
│   ├── session.json           # RecordingSession metadata
│   ├── chunk-1711187200000.bin  # 1-minute chunk (protobuf binary)
│   ├── chunk-1711187260000.bin
│   └── chunk-1711187320000.bin
├── {session-id-2}/
│   ├── session.json
│   └── chunk-*.bin
└── ...
```

## Validation Rules

- Session Label: non-empty string, max 200 characters
- TimeLimitMinutes: 1..1440 (1 minute to 24 hours)
- SizeLimitBytes: 1 MB .. 10 GB
- Only one session may have Status = `Recording` at any time
- ChunkFile entries are strictly time-ordered within each file
- ChunkFiles within a session are strictly time-ordered by filename
