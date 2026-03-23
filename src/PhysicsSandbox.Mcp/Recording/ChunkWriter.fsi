module PhysicsSandbox.Mcp.Recording.ChunkWriter

open System
open PhysicsSandbox.Mcp.Recording.Types

type ChunkWriterConfig =
    { SessionDir: string
      TimeLimitMinutes: int
      SizeLimitBytes: int64 }

[<Sealed>]
type ChunkWriter =
    member Enqueue: entry: LogEntry -> unit
    member Start: unit -> unit
    member Stop: unit -> Async<unit>
    member CurrentSizeBytes: int64
    member ChunkCount: int
    member SnapshotCount: int64
    member EventCount: int64
    interface IDisposable

val create: config: ChunkWriterConfig -> ChunkWriter
