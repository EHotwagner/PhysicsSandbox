module PhysicsViewer.CameraController

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Input
open PhysicsSandbox.Shared.Contracts

/// <summary>
/// Represents the current state of the orbit camera, including position, look-at target, up vector, and zoom level.
/// </summary>
type CameraState =
    { /// <summary>The world-space position of the camera eye.</summary>
      Position: Vector3
      /// <summary>The world-space point the camera looks at.</summary>
      Target: Vector3
      /// <summary>The camera's up direction vector used for orientation.</summary>
      Up: Vector3
      /// <summary>The zoom multiplier applied to the camera distance from the target (1.0 = default).</summary>
      ZoomLevel: float }

/// <summary>
/// Creates a default camera state positioned at (10, 8, 10) looking at the origin with Y-up and zoom level 1.0.
/// </summary>
/// <returns>A new CameraState with default orbit parameters.</returns>
let defaultCamera () =
    { Position = Vector3(10f, 8f, 10f)
      Target = Vector3.Zero
      Up = Vector3.UnitY
      ZoomLevel = 1.0 }

let private protoVec3ToStride (v: Vec3) =
    if isNull v then Vector3.Zero
    else Vector3(float32 v.X, float32 v.Y, float32 v.Z)

/// <summary>
/// Applies a SetCamera gRPC command, overriding the camera's position, target, and optionally up vector.
/// </summary>
/// <param name="cmd">The SetCamera command containing new position, target, and up vectors.</param>
/// <param name="state">The current camera state to update.</param>
/// <returns>An updated CameraState with the command's values applied.</returns>
let applySetCamera (cmd: SetCamera) (state: CameraState) =
    { state with
        Position = protoVec3ToStride cmd.Position
        Target = protoVec3ToStride cmd.Target
        Up =
            let up = protoVec3ToStride cmd.Up
            if up = Vector3.Zero then state.Up else up }

/// <summary>
/// Applies a SetZoom gRPC command, updating the camera's zoom level.
/// </summary>
/// <param name="cmd">The SetZoom command containing the new zoom level.</param>
/// <param name="state">The current camera state to update.</param>
/// <returns>An updated CameraState with the new zoom level.</returns>
let applySetZoom (cmd: SetZoom) (state: CameraState) =
    { state with ZoomLevel = cmd.Level }

let private cross (a: Vector3) (b: Vector3) =
    let mutable left = a
    let mutable right = b
    let mutable result = Unchecked.defaultof<Vector3>
    Vector3.Cross(&left, &right, &result)
    result

/// <summary>
/// Processes mouse and keyboard input to orbit, pan, and zoom the camera interactively.
/// Left-drag orbits, middle-drag pans, and scroll wheel zooms.
/// </summary>
/// <param name="input">The Stride InputManager providing mouse/keyboard state.</param>
/// <param name="dt">Frame delta time in seconds, used to scale input sensitivity.</param>
/// <param name="state">The current camera state to transform.</param>
/// <returns>An updated CameraState reflecting the user's input.</returns>
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

/// <summary>
/// Applies the camera state to a Stride camera entity's transform, computing the final eye position
/// with zoom scaling and setting the look-at rotation.
/// </summary>
/// <param name="state">The camera state containing position, target, up, and zoom.</param>
/// <param name="entity">The Stride Entity whose Transform will be updated.</param>
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

/// <summary>Gets the camera's current world-space position.</summary>
let position (state: CameraState) = state.Position

/// <summary>Gets the camera's current look-at target point.</summary>
let target (state: CameraState) = state.Target

/// <summary>Gets the camera's current zoom level multiplier.</summary>
let zoomLevel (state: CameraState) = state.ZoomLevel
