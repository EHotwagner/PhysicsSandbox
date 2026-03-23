module PhysicsSandbox.Mcp.Recording.ChunkReader

open System
open PhysicsSandbox.Mcp.Recording.Types

/// Read all entries from a session directory within a time range.
val readEntries: sessionDir:string -> startTime:DateTimeOffset -> endTime:DateTimeOffset -> LogEntry list

/// Read a page of entries with cursor-based pagination.
val readPage: sessionDir:string -> startTime:DateTimeOffset -> endTime:DateTimeOffset -> cursor:PaginationCursor option -> pageSize:int -> (LogEntry list * PaginationCursor option)
