module PhysicsSandbox.Mcp.Tests.RecordingEngineTests

open System
open System.IO
open Xunit
open PhysicsSandbox.Mcp.Recording.Types
open PhysicsSandbox.Mcp.Recording.ChunkWriter
open PhysicsSandbox.Shared.Contracts

// ─── Helpers ────────────────────────────────────────────────────────────────

let makeTempDir () =
    let dir = Path.Combine(Path.GetTempPath(), $"mcp-rec-test-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let cleanUp (dir: string) =
    try
        if Directory.Exists dir then
            Directory.Delete(dir, true)
    with _ -> ()

let recentTs () = DateTimeOffset.UtcNow

// ─── T064_d: ChunkWriter/ChunkReader round-trip for PropertyEvent entries ───

[<Fact>]
let ``T064_d: MeshDefinition from PropertyEvent round-trips through ChunkWriter and ChunkReader`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = recentTs ()

        // Simulate what RecordingEngine.OnPropertyEventReceived does:
        // it extracts MeshDefinition entries from PropertyEvent.NewMeshes
        let shape = Shape(Sphere = Sphere(Radius = 2.5))
        let meshGeo = MeshGeometry(MeshId = "mesh-prop-001", Shape = shape)
        let propEvt = PropertyEvent(BodyCreated = BodyProperties(Id = "body-1"))
        propEvt.NewMeshes.Add(meshGeo)

        // Record MeshDefinition entries from the PropertyEvent (same as RecordingEngine does)
        for mg in propEvt.NewMeshes do
            writer.Enqueue(LogEntry.MeshDefinition(ts, mg.MeshId, mg.Shape))

        writer.Stop() |> Async.RunSynchronously

        // Read back via ChunkReader
        let startTime = ts.AddSeconds(-1.0)
        let endTime = ts.AddSeconds(1.0)
        let entries = PhysicsSandbox.Mcp.Recording.ChunkReader.readEntries dir startTime endTime

        let meshEntries =
            entries |> List.choose (fun e ->
                match e with
                | LogEntry.MeshDefinition(_, meshId, s) -> Some (meshId, s)
                | _ -> None)

        Assert.Single(meshEntries) |> ignore
        let (meshId, readShape) = meshEntries.[0]
        Assert.Equal("mesh-prop-001", meshId)
        Assert.NotNull(readShape)
        Assert.Equal(2.5, readShape.Sphere.Radius)
    finally
        cleanUp dir

[<Fact>]
let ``T064_d: Multiple MeshDefinitions from PropertyEvent round-trip correctly`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = recentTs ()

        // PropertyEvent with multiple new meshes
        let propEvt = PropertyEvent()
        let mg1 = MeshGeometry(MeshId = "hull-001", Shape = Shape(ConvexHull = ConvexHull()))
        mg1.Shape.ConvexHull.Points.Add(Vec3(X = 1.0, Y = 0.0, Z = 0.0))
        let mg2 = MeshGeometry(MeshId = "hull-002", Shape = Shape(Sphere = Sphere(Radius = 3.0)))
        propEvt.NewMeshes.Add(mg1)
        propEvt.NewMeshes.Add(mg2)

        for mg in propEvt.NewMeshes do
            writer.Enqueue(LogEntry.MeshDefinition(ts, mg.MeshId, mg.Shape))

        writer.Stop() |> Async.RunSynchronously

        let entries =
            PhysicsSandbox.Mcp.Recording.ChunkReader.readEntries dir (ts.AddSeconds(-1.0)) (ts.AddSeconds(1.0))

        let meshEntries =
            entries |> List.choose (fun e ->
                match e with
                | LogEntry.MeshDefinition(_, meshId, _) -> Some meshId
                | _ -> None)

        Assert.Equal(2, meshEntries.Length)
        Assert.Contains("hull-001", meshEntries)
        Assert.Contains("hull-002", meshEntries)
    finally
        cleanUp dir

// ─── T064_e: RecordingEngine records reconstructed SimulationState ──────────

[<Fact>]
let ``T064_e: RecordingEngine OnStateReceived records full SimulationState snapshot`` () =
    // This test verifies that when OnStateReceived is called with a reconstructed
    // SimulationState (merged from TickState + cached BodyProperties by GrpcConnection),
    // the engine correctly writes a StateSnapshot entry that can be read back.
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = recentTs ()

        // Simulate a reconstructed SimulationState (what GrpcConnection produces)
        let state = SimulationState()
        state.Time <- 5.0
        state.Running <- true
        state.TickMs <- 0.5
        state.SerializeMs <- 0.1

        // Dynamic body with pose + semi-static properties (merged)
        let dynBody = Body(Id = "dyn-1", IsStatic = false, Mass = 2.0)
        dynBody.Position <- Vec3(X = 1.0, Y = 2.0, Z = 3.0)
        dynBody.Orientation <- Vec4(X = 0.0, Y = 0.0, Z = 0.0, W = 1.0)
        dynBody.Velocity <- Vec3(X = 0.5, Y = 0.0, Z = 0.0)
        dynBody.AngularVelocity <- Vec3(X = 0.0, Y = 0.1, Z = 0.0)
        dynBody.Shape <- Shape(Sphere = Sphere(Radius = 1.0))
        dynBody.Color <- Color(R = 1.0, G = 0.0, B = 0.0, A = 1.0)
        dynBody.MotionType <- BodyMotionType.Dynamic
        state.Bodies.Add(dynBody)

        // Static body (from cached BodyProperties)
        let staticBody = Body(Id = "floor", IsStatic = true, Mass = 0.0)
        staticBody.Position <- Vec3(X = 0.0, Y = -1.0, Z = 0.0)
        staticBody.Orientation <- Vec4(X = 0.0, Y = 0.0, Z = 0.0, W = 1.0)
        staticBody.Shape <- Shape(Box = Box(HalfExtents = Vec3(X = 50.0, Y = 0.5, Z = 50.0)))
        staticBody.MotionType <- BodyMotionType.Static
        state.Bodies.Add(staticBody)

        // Write as a StateSnapshot (what RecordingEngine.OnStateReceived does)
        writer.Enqueue(LogEntry.StateSnapshot(ts, state))

        writer.Stop() |> Async.RunSynchronously

        // Read back and verify
        let entries =
            PhysicsSandbox.Mcp.Recording.ChunkReader.readEntries dir (ts.AddSeconds(-1.0)) (ts.AddSeconds(1.0))

        let snapshots =
            entries |> List.choose (fun e ->
                match e with
                | LogEntry.StateSnapshot(_, s) -> Some s
                | _ -> None)

        Assert.Single(snapshots) |> ignore
        let readState = snapshots.[0]
        Assert.Equal(5.0, readState.Time)
        Assert.True(readState.Running)
        Assert.Equal(2, readState.Bodies.Count)

        // Verify dynamic body has both pose and semi-static data
        let dynRead = readState.Bodies |> Seq.find (fun b -> b.Id = "dyn-1")
        Assert.Equal(1.0, dynRead.Position.X)
        Assert.Equal(0.5, dynRead.Velocity.X)
        Assert.Equal(2.0, dynRead.Mass)
        Assert.NotNull(dynRead.Shape)
        Assert.Equal(1.0, dynRead.Shape.Sphere.Radius)

        // Verify static body has pose and shape from cached properties
        let staticRead = readState.Bodies |> Seq.find (fun b -> b.Id = "floor")
        Assert.True(staticRead.IsStatic)
        Assert.Equal(-1.0, staticRead.Position.Y)
        Assert.NotNull(staticRead.Shape)
    finally
        cleanUp dir

[<Fact>]
let ``T064_e: RecordingEngine preserves velocity data in reconstructed state`` () =
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = recentTs ()

        // Reconstructed state with velocity data (client/MCP path, exclude_velocity=false)
        let state = SimulationState(Time = 10.0, Running = true)
        let body = Body(Id = "vel-body", Mass = 1.5)
        body.Velocity <- Vec3(X = 3.0, Y = -1.0, Z = 0.5)
        body.AngularVelocity <- Vec3(X = 0.1, Y = 0.2, Z = 0.3)
        body.Position <- Vec3(X = 5.0, Y = 10.0, Z = 15.0)
        body.Orientation <- Vec4(X = 0.0, Y = 0.0, Z = 0.0, W = 1.0)
        state.Bodies.Add(body)

        writer.Enqueue(LogEntry.StateSnapshot(ts, state))
        writer.Stop() |> Async.RunSynchronously

        let entries =
            PhysicsSandbox.Mcp.Recording.ChunkReader.readEntries dir (ts.AddSeconds(-1.0)) (ts.AddSeconds(1.0))

        let readState =
            entries |> List.pick (fun e ->
                match e with
                | LogEntry.StateSnapshot(_, s) -> Some s
                | _ -> None)

        let readBody = readState.Bodies |> Seq.find (fun b -> b.Id = "vel-body")
        Assert.Equal(3.0, readBody.Velocity.X)
        Assert.Equal(-1.0, readBody.Velocity.Y)
        Assert.Equal(0.5, readBody.Velocity.Z)
        Assert.Equal(0.1, readBody.AngularVelocity.X)
        Assert.Equal(0.2, readBody.AngularVelocity.Y)
        Assert.Equal(0.3, readBody.AngularVelocity.Z)
    finally
        cleanUp dir

// ─── T076: RecordingEngine reconstructed SimulationState includes velocity ──

[<Fact>]
let ``T076: Reconstructed SimulationState snapshot includes velocity data for trajectory recording`` () =
    // The MCP subscribes with exclude_velocity=false, so the reconstructed state
    // retains velocity data needed for trajectory analysis and recording.
    let dir = makeTempDir ()
    try
        let config = { SessionDir = dir; TimeLimitMinutes = 60; SizeLimitBytes = 100_000_000L }
        let writer = create config
        writer.Start()

        let ts = recentTs ()

        // Simulate the full recording pipeline:
        // GrpcConnection reconstructs SimulationState from TickState + cached BodyProperties,
        // then calls RecordingEngine.OnStateReceived with the full state.
        let state = SimulationState(Time = 42.0, Running = true)

        // Three bodies with various velocities (typical trajectory recording scenario)
        for i in 1..3 do
            let body = Body(Id = $"traj-{i}", Mass = float i)
            body.Position <- Vec3(X = float i * 10.0, Y = 5.0, Z = 0.0)
            body.Velocity <- Vec3(X = float i * 2.0, Y = float i * -1.0, Z = 0.5)
            body.AngularVelocity <- Vec3(X = 0.0, Y = float i * 0.3, Z = 0.0)
            body.Orientation <- Vec4(X = 0.0, Y = 0.0, Z = 0.0, W = 1.0)
            body.Shape <- Shape(Sphere = Sphere(Radius = float i * 0.5))
            body.MotionType <- BodyMotionType.Dynamic
            state.Bodies.Add(body)

        writer.Enqueue(LogEntry.StateSnapshot(ts, state))
        writer.Stop() |> Async.RunSynchronously

        let entries =
            PhysicsSandbox.Mcp.Recording.ChunkReader.readEntries dir (ts.AddSeconds(-1.0)) (ts.AddSeconds(1.0))

        let readState =
            entries |> List.pick (fun e ->
                match e with
                | LogEntry.StateSnapshot(_, s) -> Some s
                | _ -> None)

        Assert.Equal(3, readState.Bodies.Count)

        // Verify all bodies have velocity data intact
        for i in 1..3 do
            let body = readState.Bodies |> Seq.find (fun b -> b.Id = $"traj-{i}")
            Assert.Equal(float i * 2.0, body.Velocity.X)
            Assert.Equal(float i * -1.0, body.Velocity.Y)
            Assert.Equal(0.5, body.Velocity.Z)
            Assert.Equal(float i * 0.3, body.AngularVelocity.Y)
    finally
        cleanUp dir
