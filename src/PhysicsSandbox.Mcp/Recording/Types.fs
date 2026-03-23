module PhysicsSandbox.Mcp.Recording.Types

open System
open System.Text.Json

[<RequireQualifiedAccess>]
type EntryType =
    | StateSnapshot    = 0uy
    | CommandEvent     = 1uy
    | MeshDefinition   = 2uy
    | MeshFetchEvent   = 3uy

[<RequireQualifiedAccess>]
type SessionStatus =
    | Recording
    | Completed
    | Failed

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

type ChunkInfo =
    { FileName: string
      StartTime: DateTimeOffset
      EndTime: DateTimeOffset
      SizeBytes: int64
      EntryCount: int }

type PaginationCursor =
    { ChunkFileName: string
      ByteOffset: int64 }

[<RequireQualifiedAccess>]
type LogEntry =
    | StateSnapshot    of timestamp: DateTimeOffset * state: PhysicsSandbox.Shared.Contracts.SimulationState
    | CommandEvent     of timestamp: DateTimeOffset * event: PhysicsSandbox.Shared.Contracts.CommandEvent
    | MeshDefinition   of timestamp: DateTimeOffset * meshId: string * shape: PhysicsSandbox.Shared.Contracts.Shape
    | MeshFetchEvent   of timestamp: DateTimeOffset * requestedIds: string list * hits: int * misses: int * missedIds: string list

module WireFormat =
    let HeaderSize = 4 + 8 + 1 // uint32 totalSize + int64 timestampMs + byte entryType

    let encodeCursor (cursor: PaginationCursor) : string =
        let json = JsonSerializer.Serialize({| chunk = cursor.ChunkFileName; offset = cursor.ByteOffset |})
        Convert.ToBase64String(Text.Encoding.UTF8.GetBytes(json))

    let decodeCursor (encoded: string) : PaginationCursor option =
        try
            let json = Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded))
            let doc = JsonDocument.Parse(json)
            let root = doc.RootElement
            Some { ChunkFileName = root.GetProperty("chunk").GetString()
                   ByteOffset = root.GetProperty("offset").GetInt64() }
        with _ -> None
