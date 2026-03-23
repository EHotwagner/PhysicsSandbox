module PhysicsSandbox.Mcp.Recording.ChunkWriter

open System
open System.IO
open System.Threading
open System.Threading.Channels
open Google.Protobuf
open PhysicsSandbox.Mcp.Recording.Types
open PhysicsSandbox.Shared.Contracts

type ChunkWriterConfig =
    { SessionDir: string
      TimeLimitMinutes: int
      SizeLimitBytes: int64 }

[<Sealed>]
type ChunkWriter(config: ChunkWriterConfig) =
    let channel =
        Channel.CreateBounded<LogEntry>(
            BoundedChannelOptions(10000, FullMode = BoundedChannelFullMode.DropOldest))

    let mutable currentStream: FileStream option = None
    let mutable currentChunkMinute: int64 = -1L
    let mutable currentSizeBytes: int64 = 0L
    let mutable chunkCount: int = 0
    let mutable snapshotCount: int64 = 0L
    let mutable eventCount: int64 = 0L
    let mutable consumerTask: Tasks.Task = null
    let mutable disposed = false

    let truncateToMinute (ms: int64) = ms / 60000L * 60000L

    let openChunk (timestampMs: int64) =
        currentStream |> Option.iter (fun s -> s.Flush(); s.Dispose())
        let chunkMinuteMs = truncateToMinute timestampMs
        let fileName = sprintf "chunk-%d.bin" timestampMs
        let path = Path.Combine(config.SessionDir, fileName)
        let stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read)
        currentStream <- Some stream
        currentChunkMinute <- chunkMinuteMs
        chunkCount <- chunkCount + 1

    let pruneOldChunks () =
        try
            let files =
                Directory.GetFiles(config.SessionDir, "chunk-*.bin")
                |> Array.sortBy Path.GetFileName

            if files.Length = 0 then ()
            else

            let nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            let timeCutoffMs = nowMs - (int64 config.TimeLimitMinutes * 60000L)
            let mutable remaining = ResizeArray(files)
            let mutable pruned = 0

            // Time-based pruning
            let toRemove = ResizeArray()
            for f in remaining do
                let name = Path.GetFileNameWithoutExtension(f)
                match name.Replace("chunk-", "") |> Int64.TryParse with
                | true, chunkTs when chunkTs < timeCutoffMs ->
                    toRemove.Add(f)
                | _ -> ()

            for f in toRemove do
                try
                    let size = FileInfo(f).Length
                    File.Delete(f)
                    currentSizeBytes <- currentSizeBytes - size
                    pruned <- pruned + 1
                    remaining.Remove(f) |> ignore
                with ex ->
                    eprintfn "[ChunkWriter] Failed to prune %s: %s" (Path.GetFileName f) ex.Message

            // Size-based pruning
            while currentSizeBytes > config.SizeLimitBytes && remaining.Count > 1 do
                let oldest = remaining.[0]
                try
                    let size = FileInfo(oldest).Length
                    File.Delete(oldest)
                    currentSizeBytes <- currentSizeBytes - size
                    pruned <- pruned + 1
                    remaining.RemoveAt(0)
                with ex ->
                    eprintfn "[ChunkWriter] Failed to prune %s: %s" (Path.GetFileName oldest) ex.Message
                    remaining.RemoveAt(0) // avoid infinite loop

            chunkCount <- remaining.Count

            if pruned > 0 then
                eprintfn "[ChunkWriter] Pruned %d chunk(s), remaining size: %d bytes, chunks: %d"
                    pruned currentSizeBytes chunkCount
        with ex ->
            eprintfn "[ChunkWriter] Pruning error: %s" ex.Message

    let writeEntry (entry: LogEntry) =
        let timestampMs, entryTypeByte, payload =
            match entry with
            | LogEntry.StateSnapshot(ts, state) ->
                snapshotCount <- snapshotCount + 1L
                ts.ToUnixTimeMilliseconds(), EntryType.StateSnapshot, state.ToByteArray()
            | LogEntry.CommandEvent(ts, evt) ->
                eventCount <- eventCount + 1L
                ts.ToUnixTimeMilliseconds(), EntryType.CommandEvent, evt.ToByteArray()

        let entryMinute = truncateToMinute timestampMs

        let rotated =
            if currentChunkMinute <> entryMinute then
                openChunk timestampMs
                true
            else
                match currentStream with
                | None -> openChunk timestampMs; true
                | Some _ -> false

        if rotated then
            pruneOldChunks ()

        let stream = currentStream.Value
        let totalSize = 8 + 1 + payload.Length
        use writer = new BinaryWriter(stream, Text.Encoding.UTF8, leaveOpen = true)
        writer.Write(uint32 totalSize)
        writer.Write(timestampMs)
        writer.Write(byte entryTypeByte)
        writer.Write(payload)
        writer.Flush()

        currentSizeBytes <- currentSizeBytes + int64 (4 + totalSize)

    let consume () =
        Tasks.Task.Run(fun () ->
            let reader = channel.Reader
            let mutable cont = true
            while cont do
                let waitTask = reader.WaitToReadAsync(CancellationToken.None)
                let available = waitTask.AsTask().GetAwaiter().GetResult()
                if available then
                    let mutable entry = Unchecked.defaultof<LogEntry>
                    while reader.TryRead(&entry) do
                        try
                            writeEntry entry
                        with ex ->
                            eprintfn "[ChunkWriter] Error writing entry: %s" ex.Message
                else
                    cont <- false)

    member _.Enqueue(entry: LogEntry) =
        channel.Writer.TryWrite(entry) |> ignore

    member _.Start() =
        if not (Directory.Exists(config.SessionDir)) then
            Directory.CreateDirectory(config.SessionDir) |> ignore
        consumerTask <- consume ()

    member _.Stop() =
        async {
            channel.Writer.Complete()
            if consumerTask <> null then
                do! consumerTask |> Async.AwaitTask
            currentStream |> Option.iter (fun s -> s.Flush(); s.Dispose())
            currentStream <- None
        }

    member _.CurrentSizeBytes = currentSizeBytes
    member _.ChunkCount = chunkCount
    member _.SnapshotCount = snapshotCount
    member _.EventCount = eventCount

    interface IDisposable with
        member this.Dispose() =
            if not disposed then
                disposed <- true
                try
                    channel.Writer.TryComplete() |> ignore
                    currentStream |> Option.iter (fun s ->
                        try s.Flush(); s.Dispose() with _ -> ())
                    currentStream <- None
                with _ -> ()

let create (config: ChunkWriterConfig) : ChunkWriter =
    new ChunkWriter(config)
