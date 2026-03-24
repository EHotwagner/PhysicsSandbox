module PhysicsClient.Tests.ViewCommandsTests

open Xunit
open PhysicsSandbox.Shared.Contracts

[<Fact>]
let ``SmoothCamera message has correct fields`` () =
    let sc = SmoothCamera()
    sc.Position <- Vec3(X = 1.0, Y = 2.0, Z = 3.0)
    sc.Target <- Vec3(X = 4.0, Y = 5.0, Z = 6.0)
    sc.Up <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    sc.DurationSeconds <- 2.0
    sc.ZoomLevel <- 1.5
    Assert.Equal(1.0, sc.Position.X)
    Assert.Equal(2.0, sc.Position.Y)
    Assert.Equal(3.0, sc.Position.Z)
    Assert.Equal(4.0, sc.Target.X)
    Assert.Equal(5.0, sc.Target.Y)
    Assert.Equal(6.0, sc.Target.Z)
    Assert.Equal(0.0, sc.Up.X)
    Assert.Equal(1.0, sc.Up.Y)
    Assert.Equal(0.0, sc.Up.Z)
    Assert.Equal(2.0, sc.DurationSeconds)
    Assert.Equal(1.5, sc.ZoomLevel)

[<Fact>]
let ``SmoothCamera defaults zoom to zero`` () =
    let sc = SmoothCamera()
    sc.Position <- Vec3(X = 1.0, Y = 2.0, Z = 3.0)
    sc.Target <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    sc.DurationSeconds <- 1.0
    Assert.Equal(0.0, sc.ZoomLevel)

[<Fact>]
let ``ViewCommand wraps SmoothCamera correctly`` () =
    let sc = SmoothCamera()
    sc.Position <- Vec3(X = 10.0, Y = 20.0, Z = 30.0)
    sc.Target <- Vec3(X = 0.0, Y = 0.0, Z = 0.0)
    sc.Up <- Vec3(X = 0.0, Y = 1.0, Z = 0.0)
    sc.DurationSeconds <- 3.5
    sc.ZoomLevel <- 2.0
    let cmd = ViewCommand()
    cmd.SmoothCamera <- sc
    Assert.NotNull(cmd.SmoothCamera)
    Assert.Equal(10.0, cmd.SmoothCamera.Position.X)
    Assert.Equal(20.0, cmd.SmoothCamera.Position.Y)
    Assert.Equal(30.0, cmd.SmoothCamera.Position.Z)
    Assert.Equal(3.5, cmd.SmoothCamera.DurationSeconds)
    Assert.Equal(2.0, cmd.SmoothCamera.ZoomLevel)

[<Fact>]
let ``SetNarration message stores text`` () =
    let sn = SetNarration()
    sn.Text <- "Hello, physics world!"
    Assert.Equal("Hello, physics world!", sn.Text)

[<Fact>]
let ``SetNarration empty text clears narration`` () =
    let sn = SetNarration()
    sn.Text <- ""
    Assert.Equal("", sn.Text)

[<Fact>]
let ``ViewCommand wraps SetNarration correctly`` () =
    let sn = SetNarration()
    sn.Text <- "Demo: Smooth Camera"
    let cmd = ViewCommand()
    cmd.SetNarration <- sn
    Assert.NotNull(cmd.SetNarration)
    Assert.Equal("Demo: Smooth Camera", cmd.SetNarration.Text)

[<Fact>]
let ``CameraLookAt message has body_id and duration`` () =
    let la = CameraLookAt()
    la.BodyId <- "sphere-1"
    la.DurationSeconds <- 1.5
    Assert.Equal("sphere-1", la.BodyId)
    Assert.Equal(1.5, la.DurationSeconds)

[<Fact>]
let ``ViewCommand wraps CameraLookAt correctly`` () =
    let la = CameraLookAt()
    la.BodyId <- "box-1"
    la.DurationSeconds <- 0.0
    let cmd = ViewCommand()
    cmd.CameraLookAt <- la
    Assert.NotNull(cmd.CameraLookAt)
    Assert.Equal("box-1", cmd.CameraLookAt.BodyId)
    Assert.Equal(0.0, cmd.CameraLookAt.DurationSeconds)

[<Fact>]
let ``CameraFollow message has body_id`` () =
    let cf = CameraFollow()
    cf.BodyId <- "tracked-body"
    Assert.Equal("tracked-body", cf.BodyId)

[<Fact>]
let ``CameraOrbit message has all fields`` () =
    let co = CameraOrbit()
    co.BodyId <- "center-body"
    co.DurationSeconds <- 5.0
    co.Degrees <- 180.0
    Assert.Equal("center-body", co.BodyId)
    Assert.Equal(5.0, co.DurationSeconds)
    Assert.Equal(180.0, co.Degrees)

[<Fact>]
let ``CameraChase message has body_id and offset`` () =
    let cc = CameraChase()
    cc.BodyId <- "chase-target"
    cc.Offset <- Vec3(X = 0.0, Y = 5.0, Z = -10.0)
    Assert.Equal("chase-target", cc.BodyId)
    Assert.Equal(0.0, cc.Offset.X)
    Assert.Equal(5.0, cc.Offset.Y)
    Assert.Equal(-10.0, cc.Offset.Z)

[<Fact>]
let ``CameraFrameBodies message holds multiple body IDs`` () =
    let fb = CameraFrameBodies()
    fb.BodyIds.Add("a")
    fb.BodyIds.Add("b")
    fb.BodyIds.Add("c")
    Assert.Equal(3, fb.BodyIds.Count)
    Assert.Equal("a", fb.BodyIds.[0])
    Assert.Equal("b", fb.BodyIds.[1])
    Assert.Equal("c", fb.BodyIds.[2])

[<Fact>]
let ``CameraShake message has intensity and duration`` () =
    let cs = CameraShake()
    cs.Intensity <- 0.8
    cs.DurationSeconds <- 0.5
    Assert.Equal(0.8, cs.Intensity)
    Assert.Equal(0.5, cs.DurationSeconds)

[<Fact>]
let ``CameraStop message can be created`` () =
    let stop = CameraStop()
    let cmd = ViewCommand()
    cmd.CameraStop <- stop
    Assert.NotNull(cmd.CameraStop)
