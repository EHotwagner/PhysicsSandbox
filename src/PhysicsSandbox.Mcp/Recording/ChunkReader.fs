module PhysicsSandbox.Mcp.Recording.ChunkReader

open System
open System.IO
open PhysicsSandbox.Mcp.Recording.Types
open PhysicsSandbox.Shared.Contracts

let private parseChunkTimestamp (fileName: string) : int64 option =
    let name = Path.GetFileNameWithoutExtension(fileName)
    match name.Replace("chunk-", "") |> Int64.TryParse with
    | true, ts -> Some ts
    | false, _ -> None

let private getChunkFiles (sessionDir: string) =
    if Directory.Exists(sessionDir) then
        Directory.GetFiles(sessionDir, "chunk-*.bin")
        |> Array.sortBy Path.GetFileName
    else
        Array.empty

let private filterChunksByTimeRange (files: string array) (startMs: int64) (endMs: int64) =
    files
    |> Array.filter (fun f ->
        match parseChunkTimestamp (Path.GetFileName f) with
        | Some chunkTs ->
            // Chunk covers from chunkTs onward (up to ~1 minute).
            // Include if chunk start is before endMs and chunk could contain entries >= startMs.
            // A chunk truncated to minute boundary could have entries spanning the minute.
            let chunkEndEstimate = chunkTs + 60000L
            chunkTs <= endMs && chunkEndEstimate >= startMs
        | None -> false)

let private deserializeEntry (timestampMs: int64) (entryType: byte) (payload: byte array) : LogEntry option =
    let ts = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs)
    try
        match entryType with
        | b when b = byte EntryType.StateSnapshot ->
            let state = SimulationState.Parser.ParseFrom(payload)
            Some (LogEntry.StateSnapshot(ts, state))
        | b when b = byte EntryType.CommandEvent ->
            let evt = CommandEvent.Parser.ParseFrom(payload)
            Some (LogEntry.CommandEvent(ts, evt))
        | _ -> None
    with _ -> None

let private readEntriesFromStream (reader: BinaryReader) (stream: FileStream) (startMs: int64) (endMs: int64) (limit: int) : LogEntry list * int64 =
    let entries = ResizeArray()
    let mutable lastPosition = stream.Position
    try
        while stream.Position < stream.Length && entries.Count < limit do
            lastPosition <- stream.Position
            let totalSize = reader.ReadUInt32() |> int
            if totalSize < 9 then
                // invalid entry, skip
                stream.Position <- stream.Length
            else
                let timestampMs = reader.ReadInt64()
                let entryType = reader.ReadByte()
                let payloadSize = totalSize - 8 - 1
                if payloadSize < 0 || stream.Position + int64 payloadSize > stream.Length then
                    stream.Position <- stream.Length
                else
                    let payload = reader.ReadBytes(payloadSize)
                    if timestampMs >= startMs && timestampMs <= endMs then
                        match deserializeEntry timestampMs entryType payload with
                        | Some entry -> entries.Add(entry)
                        | None -> ()
                    elif timestampMs > endMs then
                        stream.Position <- stream.Length // past range, stop
    with
    | :? EndOfStreamException -> ()
    | _ -> ()
    entries |> Seq.toList, stream.Position

let readEntries (sessionDir: string) (startTime: DateTimeOffset) (endTime: DateTimeOffset) : LogEntry list =
    let startMs = startTime.ToUnixTimeMilliseconds()
    let endMs = endTime.ToUnixTimeMilliseconds()
    let files = getChunkFiles sessionDir |> fun fs -> filterChunksByTimeRange fs startMs endMs

    [   for file in files do
            use stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            use reader = new BinaryReader(stream, Text.Encoding.UTF8, leaveOpen = true)
            let entries, _ = readEntriesFromStream reader stream startMs endMs Int32.MaxValue
            yield! entries ]

let readPage (sessionDir: string) (startTime: DateTimeOffset) (endTime: DateTimeOffset) (cursor: PaginationCursor option) (pageSize: int) : (LogEntry list * PaginationCursor option) =
    let startMs = startTime.ToUnixTimeMilliseconds()
    let endMs = endTime.ToUnixTimeMilliseconds()
    let allFiles = getChunkFiles sessionDir |> fun fs -> filterChunksByTimeRange fs startMs endMs

    if allFiles.Length = 0 then
        [], None
    else

    // Determine starting chunk and offset from cursor
    let startFileIdx, startOffset =
        match cursor with
        | Some c ->
            let idx = allFiles |> Array.tryFindIndex (fun f -> Path.GetFileName(f) = c.ChunkFileName)
            match idx with
            | Some i -> i, c.ByteOffset
            | None -> 0, 0L
        | None -> 0, 0L

    let entries = ResizeArray()
    let mutable nextCursor: PaginationCursor option = None
    let mutable fileIdx = startFileIdx
    let mutable remaining = pageSize

    while fileIdx < allFiles.Length && remaining > 0 do
        let file = allFiles.[fileIdx]
        use stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        use reader = new BinaryReader(stream, Text.Encoding.UTF8, leaveOpen = true)

        if fileIdx = startFileIdx && startOffset > 0L && startOffset < stream.Length then
            stream.Position <- startOffset

        let pageEntries, endPos = readEntriesFromStream reader stream startMs endMs remaining
        entries.AddRange(pageEntries)
        remaining <- remaining - pageEntries.Length

        if remaining <= 0 && endPos < stream.Length then
            // More entries in this chunk
            nextCursor <- Some { ChunkFileName = Path.GetFileName(file); ByteOffset = endPos }
        elif remaining <= 0 && fileIdx + 1 < allFiles.Length then
            // Move to next chunk
            nextCursor <- Some { ChunkFileName = Path.GetFileName(allFiles.[fileIdx + 1]); ByteOffset = 0L }

        fileIdx <- fileIdx + 1

    entries |> Seq.toList, nextCursor
