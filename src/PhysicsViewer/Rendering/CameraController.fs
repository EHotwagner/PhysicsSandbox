module PhysicsViewer.CameraController

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Input
open PhysicsSandbox.Shared.Contracts

type CameraState =
    { Position: Vector3
      Target: Vector3
      Up: Vector3
      ZoomLevel: float }

let defaultCamera () =
    { Position = Vector3(10f, 8f, 10f)
      Target = Vector3.Zero
      Up = Vector3.UnitY
      ZoomLevel = 1.0 }

let private protoVec3ToStride (v: Vec3) =
    if isNull v then Vector3.Zero
    else Vector3(float32 v.X, float32 v.Y, float32 v.Z)

let applySetCamera (cmd: SetCamera) (state: CameraState) =
    { state with
        Position = protoVec3ToStride cmd.Position
        Target = protoVec3ToStride cmd.Target
        Up =
            let up = protoVec3ToStride cmd.Up
            if up = Vector3.Zero then state.Up else up }

let applySetZoom (cmd: SetZoom) (state: CameraState) =
    { state with ZoomLevel = cmd.Level }

let private cross (a: Vector3) (b: Vector3) =
    let mutable left = a
    let mutable right = b
    let mutable result = Unchecked.defaultof<Vector3>
    Vector3.Cross(&left, &right, &result)
    result

let applyInput (input: InputManager) (dt: float32) (state: CameraState) =
    let mutable pos = state.Position
    let mutable tgt = state.Target

    // Compute offset from target
    let mutable offset = Unchecked.defaultof<Vector3>
    Vector3.Subtract(&pos, &tgt, &offset)

    // Orbit: left mouse drag
    if input.IsMouseButtonDown(MouseButton.Left) then
        let delta = input.MouseDelta
        let yaw = -delta.X * 3f * dt
        let pitch = -delta.Y * 3f * dt

        let mutable rotY = Unchecked.defaultof<Matrix>
        Matrix.RotationY(yaw, &rotY)
        Vector3.TransformCoordinate(&offset, &rotY, &offset)

        let mutable rightVec = cross offset state.Up
        if rightVec.LengthSquared() > 0.001f then
            let mutable rotRight = Unchecked.defaultof<Matrix>
            rightVec.Normalize()
            Matrix.RotationAxis(&rightVec, pitch, &rotRight)
            Vector3.TransformCoordinate(&offset, &rotRight, &offset)

    // Pan: middle mouse drag
    if input.IsMouseButtonDown(MouseButton.Middle) then
        let delta = input.MouseDelta
        let mutable rightVec = cross offset state.Up
        if rightVec.LengthSquared() > 0.001f then
            rightVec.Normalize()
            let panSpeed = 5f * dt
            let mutable panRight = Unchecked.defaultof<Vector3>
            Vector3.Multiply(&rightVec, -delta.X * panSpeed, &panRight)
            let mutable up = state.Up
            let mutable panUp = Unchecked.defaultof<Vector3>
            Vector3.Multiply(&up, delta.Y * panSpeed, &panUp)
            Vector3.Add(&tgt, &panRight, &tgt)
            Vector3.Add(&tgt, &panUp, &tgt)

    // Zoom: scroll wheel
    let scroll = input.MouseWheelDelta
    if abs scroll > 0.001f then
        let factor = 1f - scroll * 0.1f
        Vector3.Multiply(&offset, factor, &offset)

    Vector3.Add(&tgt, &offset, &pos)

    { state with Position = pos; Target = tgt }

let applyToCamera (state: CameraState) (entity: Entity) =
    // Apply zoom by scaling camera distance from target (FR-006)
    let mutable offset = Unchecked.defaultof<Vector3>
    let mutable pos = state.Position
    let mutable tgt = state.Target
    Vector3.Subtract(&pos, &tgt, &offset)

    let zoomFactor = float32 (if state.ZoomLevel > 0.0 then 1.0 / state.ZoomLevel else 1.0)
    Vector3.Multiply(&offset, zoomFactor, &offset)

    let mutable eye = Unchecked.defaultof<Vector3>
    Vector3.Add(&tgt, &offset, &eye)
    entity.Transform.Position <- eye

    // Compute look-at rotation
    let mutable lookAt = Unchecked.defaultof<Matrix>
    let mutable up = state.Up
    Matrix.LookAtRH(&eye, &tgt, &up, &lookAt)
    lookAt.Invert()
    let mutable rotation = Unchecked.defaultof<Quaternion>
    Quaternion.RotationMatrix(&lookAt, &rotation)
    entity.Transform.Rotation <- rotation

let position (state: CameraState) = state.Position
let target (state: CameraState) = state.Target
let zoomLevel (state: CameraState) = state.ZoomLevel
