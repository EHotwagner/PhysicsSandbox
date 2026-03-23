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

[<Fact>]
let ``write and read binary format`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = DateTimeOffset(2026, 3, 23, 12, 0, 0, TimeSpan.Zero)
        let state = makeState 1.5
        let entry = LogEntry.StateSnapshot(ts, state)
        writer.Enqueue(entry)

        writer.Stop() |> Async.RunSynchronously

        let chunkFiles = Directory.GetFiles(dir, "chunk-*.bin")
        Assert.NotEmpty(chunkFiles)

        use stream = new FileStream(chunkFiles.[0], FileMode.Open, FileAccess.Read)
        use reader = new BinaryReader(stream)

        // Read the uint32 length prefix
        let totalSize = reader.ReadUInt32()
        Assert.True(totalSize > 0u, "Total size should be > 0")

        // Read int64 timestamp
        let timestampMs = reader.ReadInt64()
        Assert.Equal(ts.ToUnixTimeMilliseconds(), timestampMs)

        // Read byte entry type
        let entryTypeByte = reader.ReadByte()
        Assert.Equal(byte EntryType.StateSnapshot, entryTypeByte)

        // Payload size = totalSize - 8 (timestamp) - 1 (entryType)
        let payloadSize = int totalSize - 8 - 1
        let payload = reader.ReadBytes(payloadSize)

        // Deserialize payload back to SimulationState
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

        // Two entries 1 minute apart — should land in different chunks
        let ts1 = DateTimeOffset(2026, 3, 23, 12, 0, 0, TimeSpan.Zero)
        let ts2 = DateTimeOffset(2026, 3, 23, 12, 1, 0, TimeSpan.Zero)
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

        // 5 entries all within the same minute
        let baseTs = DateTimeOffset(2026, 3, 23, 12, 0, 0, TimeSpan.Zero)
        for i in 0..4 do
            let ts = baseTs.AddSeconds(float i * 10.0)
            writer.Enqueue(makeLogEntry ts)

        writer.Stop() |> Async.RunSynchronously

        let chunkFiles = Directory.GetFiles(dir, "chunk-*.bin")
        Assert.Equal(1, chunkFiles.Length)

        let fileSize = FileInfo(chunkFiles.[0]).Length
        // Each entry has at least HeaderSize (13) bytes, so 5 entries > 5 * 13
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

        let ts = DateTimeOffset(2026, 3, 23, 12, 0, 0, TimeSpan.Zero)
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

        let ts = DateTimeOffset(2026, 3, 23, 12, 0, 0, TimeSpan.Zero)
        for i in 0..9 do
            writer.Enqueue(makeLogEntry (ts.AddSeconds(float i)))

        writer.Stop() |> Async.RunSynchronously

        let totalFileSize =
            Directory.GetFiles(dir, "chunk-*.bin")
            |> Array.sumBy (fun f -> FileInfo(f).Length)

        // CurrentSizeBytes should match the actual file size on disk
        Assert.Equal(totalFileSize, writer.CurrentSizeBytes)
    finally
        cleanUp dir
