# Research: MCP Data Logging for Analysis

**Branch**: `005-mcp-data-logging` | **Date**: 2026-03-23

## R1: Storage Format for Recording

**Decision**: Protobuf binary serialization with length-prefixed append-only log files (one file per 1-minute chunk).

**Rationale**: Google.Protobuf is already a dependency of the MCP server. Proto messages support `ToByteArray()` / `Parser.ParseFrom()` and `CalculateSize()` (already used in metrics tracking). Length-prefixed format (`[uint32 size | byte[] message]`) is the standard protobuf streaming convention. One-file-per-minute chunking enables O(1) time-based pruning (delete oldest file) and aligns with the spec's chunk-based pruning requirement.

**Alternatives considered**:
- SQLite: Adds a new dependency (`Microsoft.Data.Sqlite`), heavier than needed for sequential append/read workloads. Better for ad-hoc queries but overkill for time-range scans over 10 minutes of data.
- LiteDB: Document-oriented, F#-friendly, but adds dependency and doesn't leverage existing protobuf types.
- JSON lines: Human-readable but ~5-10x larger than binary protobuf for physics state data. Unsuitable for 60Hz full-fidelity recording.

## R2: Storage Location

**Decision**: `~/.config/PhysicsSandbox/recordings/<session-id>/` with one subdirectory per recording session.

**Rationale**: Follows the existing convention established by PhysicsViewer's settings persistence at `~/.config/PhysicsSandbox/viewer-settings.json`. XDG-compliant on Linux. Session isolation via subdirectories enables clean deletion.

**Alternatives considered**:
- Temp directory: Data wouldn't survive reboots.
- Working directory: Non-deterministic, depends on how MCP server is launched.

## R3: Thread Safety for Recording Pipeline

**Decision**: Use `System.Threading.Channels.Channel<T>` as a bounded producer-consumer queue between the stream callbacks and the disk writer.

**Rationale**: The existing GrpcConnection uses background `Task.Run()` loops for streams. Adding synchronous disk I/O to these callbacks would block stream processing and potentially degrade MCP responsiveness (violating FR-011). A bounded channel decouples production from consumption. If the writer falls behind, the channel's `BoundedChannelFullMode.DropOldest` policy ensures the stream callbacks never block while preserving the most recent data.

**Alternatives considered**:
- Direct write in callback: Blocks stream processing, violates FR-011.
- `ConcurrentQueue<T>` + polling: Less efficient than Channel's async notification.
- `ReaderWriterLockSlim`: Appropriate for read-while-write but unnecessary — queries read finalized chunk files, not the active write buffer.

## R4: Query Implementation Strategy

**Decision**: Sequential scan of protobuf chunk files within the requested time range, with per-session metadata index for fast range selection.

**Rationale**: With a 10-minute default window at 60Hz, the maximum data volume is ~600 state snapshots × ~20KB = ~12MB of state data plus command events. Sequential scan of this volume completes well within the 2-3 second query time targets (SC-001, SC-004). A lightweight metadata file per session tracks chunk file timestamps and sizes, enabling O(1) chunk selection without scanning file contents.

**Alternatives considered**:
- In-memory index: Wouldn't survive restarts (violating FR-012).
- SQLite index: Overkill for time-ordered sequential data with known chunk boundaries.
- Binary search within chunks: Adds complexity; linear scan of 1-minute chunks is fast enough.

## R5: Session Metadata Format

**Decision**: JSON file (`session.json`) per recording session directory, following the ViewerSettings pattern with `System.Text.Json`.

**Rationale**: Mirrors the existing ViewerSettings persistence pattern. JSON is human-inspectable for debugging. Metadata is small (session ID, label, start/end time, chunk list, storage used, limits) and written infrequently (on session state changes, not per-frame).

**Alternatives considered**:
- Protobuf for metadata: Overkill for small, infrequently-updated config data. Not human-readable.
- Embedded in chunk files: Would require reading chunk files to enumerate sessions.

## R6: Pagination Cursor Strategy

**Decision**: Opaque cursor encoding chunk filename + byte offset within chunk. Returned as base64-encoded string in query responses.

**Rationale**: Cursor-based pagination (vs. offset-based) is stable across concurrent writes and pruning. Encoding the chunk file + offset allows resuming reads exactly where the previous page ended without rescanning from the beginning. Base64 encoding keeps the cursor opaque to callers while being debuggable when decoded.

**Alternatives considered**:
- Timestamp-based cursor: Doesn't uniquely identify position if multiple entries share the same timestamp.
- Numeric offset: Invalidated by pruning operations.

## R7: Auto-Start Recording Integration

**Decision**: Hook into `GrpcConnection.Start()` to begin recording when the first state stream message is received. The recording module registers as a callback/observer on the existing stream processing loops.

**Rationale**: The existing `startStateStream` and `startCommandAuditStream` functions in GrpcConnection already process incoming data in background tasks. Adding recording as a side-effect of the existing processing (via a callback/event pattern) is minimally invasive and doesn't require changing the gRPC service contracts or server-side code.

**Alternatives considered**:
- Separate gRPC stream subscription: Would double the stream connections to the server. Wasteful.
- Modifying PhysicsServer to persist: Would violate service independence (Constitution Principle I) — recording is an MCP concern, not a server concern.
