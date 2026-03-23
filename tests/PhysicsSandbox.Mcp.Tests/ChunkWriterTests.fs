module PhysicsSandbox.Mcp.Tests.ChunkWriterTests

open System
open System.IO
open Xunit
open PhysicsSandbox.Mcp.Recording.Types
open PhysicsSandbox.Mcp.Recording.ChunkWriter
open PhysicsSandbox.Shared.Contracts

let makeState (time: float) =
    let s = SimulationState()
    s.Time <- time
    s.Running <- true
    s

let makeLogEntry (ts: DateTimeOffset) =
    LogEntry.StateSnapshot(ts, makeState (float (ts.ToUnixTimeMilliseconds()) / 1000.0))

let makeTempDir () =
    let dir = Path.Combine(Path.GetTempPath(), $"mcp-test-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let cleanUp (dir: string) =
    try
        if Directory.Exists dir then
            Directory.Delete(dir, true)
    with _ -> ()

// Use recent timestamps to avoid pruning by the time-based pruner
let recentTs () = DateTimeOffset.UtcNow
let recentTsOffset (seconds: float) = DateTimeOffset.UtcNow.AddSeconds(seconds)

[<Fact>]
let ``write and read binary format`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = recentTs ()
        let state = makeState 1.5
        let entry = LogEntry.StateSnapshot(ts, state)
        writer.Enqueue(entry)

        writer.Stop() |> Async.RunSynchronously

        let chunkFiles = Directory.GetFiles(dir, "chunk-*.bin")
        Assert.NotEmpty(chunkFiles)

        use stream = new FileStream(chunkFiles.[0], FileMode.Open, FileAccess.Read)
        use reader = new BinaryReader(stream)

        let totalSize = reader.ReadUInt32()
        Assert.True(totalSize > 0u, "Total size should be > 0")

        let timestampMs = reader.ReadInt64()
        Assert.Equal(ts.ToUnixTimeMilliseconds(), timestampMs)

        let entryTypeByte = reader.ReadByte()
        Assert.Equal(byte EntryType.StateSnapshot, entryTypeByte)

        let payloadSize = int totalSize - 8 - 1
        let payload = reader.ReadBytes(payloadSize)

        let deserialized = SimulationState.Parser.ParseFrom(payload)
        Assert.Equal(1.5, deserialized.Time)
        Assert.True(deserialized.Running)
    finally
        cleanUp dir

[<Fact>]
let ``chunk rotation on minute boundary`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        // Two entries: one now, one 61 seconds from now — different minute boundaries
        let ts1 = recentTs ()
        // Ensure ts2 is in a different minute by adding enough to cross the boundary
        let minuteMs = ts1.ToUnixTimeMilliseconds() / 60000L * 60000L
        let nextMinuteStart = DateTimeOffset.FromUnixTimeMilliseconds(minuteMs + 60000L)
        let ts2 = nextMinuteStart.AddSeconds(1.0)
        writer.Enqueue(makeLogEntry ts1)
        writer.Enqueue(makeLogEntry ts2)

        writer.Stop() |> Async.RunSynchronously

        let chunkFiles = Directory.GetFiles(dir, "chunk-*.bin")
        Assert.Equal(2, chunkFiles.Length)
    finally
        cleanUp dir

[<Fact>]
let ``multiple entries per chunk`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let baseTs = recentTs ()
        for i in 0..4 do
            let ts = baseTs.AddSeconds(float i * 1.0)
            writer.Enqueue(makeLogEntry ts)

        writer.Stop() |> Async.RunSynchronously

        let chunkFiles = Directory.GetFiles(dir, "chunk-*.bin")
        Assert.Equal(1, chunkFiles.Length)

        let fileSize = FileInfo(chunkFiles.[0]).Length
        Assert.True(fileSize > int64 (5 * WireFormat.HeaderSize), $"File size {fileSize} should be > {5 * WireFormat.HeaderSize}")
    finally
        cleanUp dir

[<Fact>]
let ``flush on stop`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = recentTs ()
        for i in 0..2 do
            writer.Enqueue(makeLogEntry (ts.AddSeconds(float i)))

        writer.Stop() |> Async.RunSynchronously

        let chunkFiles = Directory.GetFiles(dir, "chunk-*.bin")
        Assert.NotEmpty(chunkFiles)
        let fileSize = FileInfo(chunkFiles.[0]).Length
        Assert.True(fileSize > 0L, "Chunk file should have content after Stop")
    finally
        cleanUp dir

[<Fact>]
let ``size tracking accuracy`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = recentTs ()
        for i in 0..9 do
            writer.Enqueue(makeLogEntry (ts.AddSeconds(float i)))

        writer.Stop() |> Async.RunSynchronously

        let totalFileSize =
            Directory.GetFiles(dir, "chunk-*.bin")
            |> Array.sumBy (fun f -> FileInfo(f).Length)

        Assert.Equal(totalFileSize, writer.CurrentSizeBytes)
    finally
        cleanUp dir

[<Fact>]
let ``MeshFetchEvent write and read round-trip`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = recentTs ()
        let entry = LogEntry.MeshFetchEvent(ts, ["mesh-001"; "mesh-002"; "mesh-003"], 2, 1, ["mesh-003"])
        writer.Enqueue(entry)

        writer.Stop() |> Async.RunSynchronously

        let chunkFiles = Directory.GetFiles(dir, "chunk-*.bin")
        Assert.NotEmpty(chunkFiles)

        // Read back and verify via ChunkReader
        let startTime = ts.AddSeconds(-1.0)
        let endTime = ts.AddSeconds(1.0)
        let entries = PhysicsSandbox.Mcp.Recording.ChunkReader.readEntries dir startTime endTime

        let fetchEvents =
            entries |> List.choose (fun e ->
                match e with
                | LogEntry.MeshFetchEvent(_, ids, h, m, missed) -> Some (ids, h, m, missed)
                | _ -> None)

        Assert.Single(fetchEvents) |> ignore
        let (ids, hits, misses, missed) = fetchEvents.[0]
        Assert.Equal(3, ids.Length)
        Assert.Contains("mesh-001", ids)
        Assert.Contains("mesh-002", ids)
        Assert.Contains("mesh-003", ids)
        Assert.Equal(2, hits)
        Assert.Equal(1, misses)
        Assert.Single(missed) |> ignore
        Assert.Equal("mesh-003", missed.[0])
    finally
        cleanUp dir
