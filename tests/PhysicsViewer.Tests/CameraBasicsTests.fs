module PhysicsViewer.Tests.CameraBasicsTests

open Xunit
open Stride.Core.Mathematics
open PhysicsSandbox.Shared.Contracts
open PhysicsViewer.CameraController

// ---------------------------------------------------------------------------
// defaultCamera
// ---------------------------------------------------------------------------

[<Fact>]
let ``defaultCamera returns expected position`` () =
    let cam = defaultCamera ()
    let pos = position cam
    Assert.Equal(10f, pos.X)
    Assert.Equal(8f, pos.Y)
    Assert.Equal(10f, pos.Z)

[<Fact>]
let ``defaultCamera returns origin as target`` () =
    let cam = defaultCamera ()
    let tgt = target cam
    Assert.Equal(Vector3.Zero, tgt)

[<Fact>]
let ``defaultCamera returns zoom 1.0`` () =
    let cam = defaultCamera ()
    Assert.Equal(1.0, zoomLevel cam)

// ---------------------------------------------------------------------------
// applySetCamera
// ---------------------------------------------------------------------------

[<Fact>]
let ``applySetCamera overrides position and target`` () =
    let cam = defaultCamera ()
    let cmd = SetCamera(
        Position = Vec3(X = 5.0, Y = 3.0, Z = 5.0),
        Target = Vec3(X = 1.0, Y = 0.0, Z = 1.0),
        Up = Vec3(X = 0.0, Y = 1.0, Z = 0.0))
    let cam2 = applySetCamera cmd cam
    let pos = position cam2
    Assert.Equal(5f, pos.X)
    Assert.Equal(3f, pos.Y)
    let tgt = target cam2
    Assert.Equal(1f, tgt.X)

// ---------------------------------------------------------------------------
// applySetZoom
// ---------------------------------------------------------------------------

[<Fact>]
let ``applySetZoom updates zoom level`` () =
    let cam = defaultCamera ()
    let cmd = SetZoom(Level = 2.5)
    let cam2 = applySetZoom cmd cam
    Assert.Equal(2.5, zoomLevel cam2)

[<Fact>]
let ``applySetCamera after applySetZoom preserves zoom`` () =
    let cam = defaultCamera ()
    let cam2 = applySetZoom (SetZoom(Level = 3.0)) cam
    let cam3 = applySetCamera (SetCamera(
        Position = Vec3(X = 1.0, Y = 2.0, Z = 3.0),
        Target = Vec3(X = 0.0, Y = 0.0, Z = 0.0),
        Up = Vec3(X = 0.0, Y = 1.0, Z = 0.0))) cam2
    Assert.Equal(3.0, zoomLevel cam3)

// ---------------------------------------------------------------------------
// T008: smoothstep tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``smoothstep 0 returns 0`` () =
    Assert.Equal(0f, smoothstep 0f)

[<Fact>]
let ``smoothstep 0.5 returns 0.5`` () =
    Assert.Equal(0.5f, smoothstep 0.5f)

[<Fact>]
let ``smoothstep 1 returns 1`` () =
    Assert.Equal(1f, smoothstep 1f)

[<Fact>]
let ``smoothstep is monotonic`` () =
    let v25 = smoothstep 0.25f
    let v50 = smoothstep 0.5f
    let v75 = smoothstep 0.75f
    Assert.True(v25 < v50)
    Assert.True(v50 < v75)

[<Fact>]
let ``smoothstep clamps negative input to 0`` () =
    Assert.Equal(0f, smoothstep -0.5f)

[<Fact>]
let ``smoothstep clamps input above 1 to 1`` () =
    Assert.Equal(1f, smoothstep 2.0f)
