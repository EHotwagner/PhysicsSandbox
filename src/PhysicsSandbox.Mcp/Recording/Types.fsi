module PhysicsSandbox.Mcp.Recording.Types

open System

/// Entry type discriminator byte for the on-disk wire format.
[<RequireQualifiedAccess>]
type EntryType =
    | StateSnapshot = 0uy
    | CommandEvent  = 1uy

/// Recording session status.
[<RequireQualifiedAccess>]
type SessionStatus =
    | Recording
    | Completed
    | Failed

/// Metadata for a recording session, persisted as session.json.
type RecordingSession =
    { Id: string
      Label: string
      Status: SessionStatus
      StartTime: DateTimeOffset
      EndTime: DateTimeOffset option
      TimeLimitMinutes: int
      SizeLimitBytes: int64
      CurrentSizeBytes: int64
      ChunkCount: int
      SnapshotCount: int64
      EventCount: int64 }

/// Metadata for a single chunk file within a session.
type ChunkInfo =
    { FileName: string
      StartTime: DateTimeOffset
      EndTime: DateTimeOffset
      SizeBytes: int64
      EntryCount: int }

/// Opaque pagination cursor for query results.
type PaginationCursor =
    { ChunkFileName: string
      ByteOffset: int64 }

/// A single recorded log entry (in-memory representation).
[<RequireQualifiedAccess>]
type LogEntry =
    | StateSnapshot  of timestamp: DateTimeOffset * state: PhysicsSandbox.Shared.Contracts.SimulationState
    | CommandEvent   of timestamp: DateTimeOffset * event: PhysicsSandbox.Shared.Contracts.CommandEvent

/// Wire format constants for binary chunk files.
/// Format: [uint32 totalSize | int64 timestampMs | byte entryType | byte[] payload]
module WireFormat =
    /// Header size in bytes: 4 (totalSize) + 8 (timestamp) + 1 (entryType) = 13
    val HeaderSize : int
    /// Encode a cursor to a base64 string.
    val encodeCursor : cursor: PaginationCursor -> string
    /// Decode a base64 string to a cursor.
    val decodeCursor : encoded: string -> PaginationCursor option
