module PhysicsViewer.Tests.CameraControllerTests

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

// ---------------------------------------------------------------------------
// T009: CameraMode state transition tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``Transitioning mode completes when elapsed exceeds duration`` () =
    let startPos = Vector3(0f, 0f, 0f)
    let endPos = Vector3(10f, 10f, 10f)
    let cam =
        defaultCamera ()
        |> setMode (Transitioning(startPos, Vector3.Zero, 1.0, endPos, Vector3.One, 2.0, 0f, 1f))
    let updated = updateCameraMode 2f Map.empty cam
    Assert.False(isActive updated)
    Assert.Equal(endPos, position updated)

[<Fact>]
let ``Transitioning with zero duration is instant snap`` () =
    let endPos = Vector3(5f, 5f, 5f)
    let endTarget = Vector3(1f, 1f, 1f)
    let cam =
        defaultCamera ()
        |> setMode (Transitioning(Vector3.Zero, Vector3.Zero, 1.0, endPos, endTarget, 2.0, 0f, 0f))
    let updated = updateCameraMode 0.016f Map.empty cam
    Assert.False(isActive updated)
    Assert.Equal(endPos, position updated)
    Assert.Equal(endTarget, target updated)

[<Fact>]
let ``cancelMode sets ActiveMode to None`` () =
    let cam =
        defaultCamera ()
        |> setMode (Following "body1")
    Assert.True(isActive cam)
    let cancelled = cancelMode cam
    Assert.False(isActive cancelled)

[<Fact>]
let ``new mode replaces existing mode`` () =
    let cam =
        defaultCamera ()
        |> setMode (Following "body1")
        |> setMode (Following "body2")
    Assert.True(isActive cam)
    let bodies = Map.ofList [ "body2", Vector3(3f, 0f, 0f) ]
    let updated = updateCameraMode 0.016f bodies cam
    Assert.Equal(Vector3(3f, 0f, 0f), target updated)

// ---------------------------------------------------------------------------
// T010: Transitioning mode interpolation tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``Transitioning interpolates to midpoint at t=0.5`` () =
    let startPos = Vector3(0f, 0f, 0f)
    let endPos = Vector3(10f, 0f, 0f)
    let cam =
        defaultCamera ()
        |> setMode (Transitioning(startPos, Vector3.Zero, 1.0, endPos, Vector3.Zero, 1.0, 0f, 2f))
    // advance 1s out of 2s total = t=0.5, smoothstep(0.5)=0.5
    let updated = updateCameraMode 1f Map.empty cam
    let pos = position updated
    Assert.Equal(5f, pos.X, 2)

[<Fact>]
let ``mid-transition cancellation preserves current position`` () =
    let startPos = Vector3(0f, 0f, 0f)
    let endPos = Vector3(10f, 0f, 0f)
    let cam =
        defaultCamera ()
        |> setMode (Transitioning(startPos, Vector3.Zero, 1.0, endPos, Vector3.Zero, 1.0, 0f, 2f))
    let midway = updateCameraMode 1f Map.empty cam
    let midPos = position midway
    let cancelled = cancelMode midway
    Assert.False(isActive cancelled)
    Assert.Equal(midPos, position cancelled)

// ---------------------------------------------------------------------------
// T018: LookingAt mode tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``LookingAt orients target toward body position`` () =
    let bodyPos = Vector3(5f, 0f, 5f)
    let bodies = Map.ofList [ "target-body", bodyPos ]
    let cam =
        defaultCamera ()
        |> setMode (LookingAt("target-body", Vector3.Zero, 0f, 0.5f))
    // complete the look-at by exceeding duration
    let updated = updateCameraMode 1f bodies cam
    Assert.Equal(bodyPos, target updated)
    Assert.False(isActive updated)

[<Fact>]
let ``LookingAt missing body ID holds position`` () =
    let cam =
        defaultCamera ()
        |> setMode (LookingAt("nonexistent", Vector3.Zero, 0f, 1f))
    let updated = updateCameraMode 0.016f Map.empty cam
    Assert.True(isActive updated)
    Assert.Equal(position cam, position updated)

// ---------------------------------------------------------------------------
// T019: Following mode tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``Following updates target to body position each frame`` () =
    let bodyPos = Vector3(3f, 1f, 2f)
    let bodies = Map.ofList [ "follow-body", bodyPos ]
    let cam =
        defaultCamera ()
        |> setMode (Following "follow-body")
    let updated = updateCameraMode 0.016f bodies cam
    Assert.Equal(bodyPos, target updated)
    Assert.True(isActive updated)

[<Fact>]
let ``Following missing body holds position`` () =
    let cam =
        defaultCamera ()
        |> setMode (Following "gone")
    let updated = updateCameraMode 0.016f Map.empty cam
    Assert.True(isActive updated)
    Assert.Equal(position cam, position updated)

// ---------------------------------------------------------------------------
// T020: Orbiting mode tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``Orbiting progresses angle over duration`` () =
    let bodyPos = Vector3(0f, 0f, 0f)
    let bodies = Map.ofList [ "orbit-body", bodyPos ]
    let cam =
        defaultCamera ()
        |> setMode (Orbiting("orbit-body", 0f, 360f, 5f, 3f, 0f, 2f))
    let mid = updateCameraMode 1f bodies cam
    Assert.True(isActive mid)
    let final' = updateCameraMode 2f bodies mid
    Assert.False(isActive final')

// ---------------------------------------------------------------------------
// T021: Chasing mode tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``Chasing position equals body plus offset`` () =
    let bodyPos = Vector3(2f, 0f, 3f)
    let offset = Vector3(0f, 5f, -3f)
    let bodies = Map.ofList [ "chase-body", bodyPos ]
    let cam =
        defaultCamera ()
        |> setMode (Chasing("chase-body", offset))
    let updated = updateCameraMode 0.016f bodies cam
    let expected = Vector3(2f, 5f, 0f)
    let pos = position updated
    Assert.Equal(expected.X, pos.X, 2)
    Assert.Equal(expected.Y, pos.Y, 2)
    Assert.Equal(expected.Z, pos.Z, 2)
    Assert.Equal(bodyPos, target updated)

// ---------------------------------------------------------------------------
// T022: Framing mode tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``Framing positions camera to keep bodies visible`` () =
    let bodies = Map.ofList [
        "body-a", Vector3(-5f, 0f, 0f)
        "body-b", Vector3(5f, 0f, 0f)
    ]
    let cam =
        defaultCamera ()
        |> setMode (Framing [ "body-a"; "body-b" ])
    let updated = updateCameraMode 0.016f bodies cam
    let pos = position updated
    Assert.True(pos.X > 0f)
    Assert.True(pos.Y > 0f)
    Assert.True(pos.Z > 0f)
    let tgt = target updated
    Assert.Equal(0f, tgt.X, 1)

[<Fact>]
let ``Framing single body works`` () =
    let bodies = Map.ofList [ "solo", Vector3(3f, 1f, 2f) ]
    let cam =
        defaultCamera ()
        |> setMode (Framing [ "solo" ])
    let updated = updateCameraMode 0.016f bodies cam
    let tgt = target updated
    Assert.Equal(3f, tgt.X, 1)
    Assert.Equal(1f, tgt.Y, 1)
    Assert.Equal(2f, tgt.Z, 1)

// ---------------------------------------------------------------------------
// T023: Shaking mode tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``Shaking duration expiry restores base position`` () =
    let basePos = Vector3(10f, 8f, 10f)
    let baseTgt = Vector3.Zero
    let cam =
        defaultCamera ()
        |> setMode (Shaking(basePos, baseTgt, 0.5f, 0f, 0.5f))
    let updated = updateCameraMode 1f Map.empty cam
    Assert.False(isActive updated)
    Assert.Equal(basePos, position updated)
    Assert.Equal(baseTgt, target updated)

// ---------------------------------------------------------------------------
// T024: Invalid body ID tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``non-existent body holds LookingAt mode`` () =
    let cam =
        defaultCamera ()
        |> setMode (LookingAt("missing", Vector3.Zero, 0f, 1f))
    let updated = updateCameraMode 0.016f Map.empty cam
    Assert.True(isActive updated)

[<Fact>]
let ``non-existent body holds Following mode`` () =
    let cam =
        defaultCamera ()
        |> setMode (Following "missing")
    let updated = updateCameraMode 0.016f Map.empty cam
    Assert.True(isActive updated)

[<Fact>]
let ``non-existent body holds Orbiting mode`` () =
    let cam =
        defaultCamera ()
        |> setMode (Orbiting("missing", 0f, 90f, 5f, 3f, 0f, 1f))
    let updated = updateCameraMode 0.016f Map.empty cam
    Assert.True(isActive updated)

[<Fact>]
let ``non-existent body holds Chasing mode`` () =
    let cam =
        defaultCamera ()
        |> setMode (Chasing("missing", Vector3.UnitY))
    let updated = updateCameraMode 0.016f Map.empty cam
    Assert.True(isActive updated)

[<Fact>]
let ``non-existent bodies holds Framing mode`` () =
    let cam =
        defaultCamera ()
        |> setMode (Framing [ "missing-a"; "missing-b" ])
    let updated = updateCameraMode 0.016f Map.empty cam
    Assert.True(isActive updated)
