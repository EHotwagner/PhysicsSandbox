module PhysicsViewer.CameraController

open System
open Stride.Core.Mathematics
open Stride.Engine
open Stride.Input
open PhysicsSandbox.Shared.Contracts

/// Discriminated union representing active camera behavior modes.
type CameraMode =
    | Transitioning of startPos: Vector3 * startTarget: Vector3 * startZoom: float * endPos: Vector3 * endTarget: Vector3 * endZoom: float * elapsed: float32 * duration: float32
    | LookingAt of bodyId: string * startTarget: Vector3 * elapsed: float32 * duration: float32
    | Following of bodyId: string
    | Orbiting of bodyId: string * startAngle: float32 * totalDegrees: float32 * radius: float32 * height: float32 * elapsed: float32 * duration: float32
    | Chasing of bodyId: string * offset: Vector3
    | Framing of bodyIds: string list
    | Shaking of basePosition: Vector3 * baseTarget: Vector3 * intensity: float32 * elapsed: float32 * duration: float32

type CameraState =
    { Position: Vector3
      Target: Vector3
      Up: Vector3
      ZoomLevel: float
      ActiveMode: CameraMode option }

/// <summary>
/// Creates a default camera state positioned at (10, 8, 10) looking at the origin with Y-up and zoom level 1.0.
/// </summary>
/// <returns>A new CameraState with default orbit parameters.</returns>
let defaultCamera () =
    { Position = Vector3(10f, 8f, 10f)
      Target = Vector3.Zero
      Up = Vector3.UnitY
      ZoomLevel = 1.0
      ActiveMode = None }

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

// ─── Smoothstep & Interpolation ─────────────────────────────────────────

/// Smoothstep easing: 3t^2 - 2t^3 (clamped to [0,1]).
let smoothstep (t: float32) =
    let t' = max 0f (min 1f t)
    t' * t' * (3f - 2f * t')

let private lerpVec3 (a: Vector3) (b: Vector3) (t: float32) =
    let mutable aa = a
    let mutable bb = b
    let mutable result = Unchecked.defaultof<Vector3>
    Vector3.Lerp(&aa, &bb, t, &result)
    result

let private lerpFloat (a: float) (b: float) (t: float32) =
    a + (b - a) * float t

// ─── Camera Mode Update ────────────────────────────────────────────────

let private rng = Random()

/// Cancel any active camera mode, keeping current interpolated position.
let cancelMode (state: CameraState) =
    { state with ActiveMode = None }

/// Advance the active camera mode by dt seconds using body positions for lookups.
let updateCameraMode (dt: float32) (bodyPositions: Map<string, Vector3>) (state: CameraState) =
    match state.ActiveMode with
    | None -> state
    | Some mode ->
        match mode with
        | Transitioning (startPos, startTarget, startZoom, endPos, endTarget, endZoom, elapsed, duration) ->
            if duration <= 0f then
                { state with Position = endPos; Target = endTarget; ZoomLevel = endZoom; ActiveMode = None }
            else
                let newElapsed = elapsed + dt
                if newElapsed >= duration then
                    { state with Position = endPos; Target = endTarget; ZoomLevel = endZoom; ActiveMode = None }
                else
                    let t = smoothstep (newElapsed / duration)
                    { state with
                        Position = lerpVec3 startPos endPos t
                        Target = lerpVec3 startTarget endTarget t
                        ZoomLevel = lerpFloat startZoom endZoom t
                        ActiveMode = Some (Transitioning (startPos, startTarget, startZoom, endPos, endTarget, endZoom, newElapsed, duration)) }

        | LookingAt (bodyId, startTarget, elapsed, duration) ->
            match Map.tryFind bodyId bodyPositions with
            | None -> state // body not yet in sim — hold position and wait
            | Some bodyPos ->
                if duration <= 0f then
                    { state with Target = bodyPos; ActiveMode = None }
                else
                    let newElapsed = elapsed + dt
                    if newElapsed >= duration then
                        { state with Target = bodyPos; ActiveMode = None }
                    else
                        let t = smoothstep (newElapsed / duration)
                        { state with
                            Target = lerpVec3 startTarget bodyPos t
                            ActiveMode = Some (LookingAt (bodyId, startTarget, newElapsed, duration)) }

        | Following bodyId ->
            match Map.tryFind bodyId bodyPositions with
            | None -> state // body not yet in sim — hold position and wait
            | Some bodyPos -> { state with Target = bodyPos }

        | Orbiting (bodyId, startAngle, totalDegrees, radius, height, elapsed, duration) ->
            match Map.tryFind bodyId bodyPositions with
            | None -> state // body not yet in sim — hold position and wait
            | Some bodyPos ->
                if duration <= 0f then
                    { state with ActiveMode = None }
                else
                    let newElapsed = elapsed + dt
                    let finished = newElapsed >= duration
                    let t = if finished then 1f else newElapsed / duration
                    let angle = startAngle + t * totalDegrees * (float32 Math.PI / 180f)
                    let x = bodyPos.X + radius * cos angle
                    let z = bodyPos.Z + radius * sin angle
                    let newPos = Vector3(x, bodyPos.Y + height, z)
                    let newMode =
                        if finished then None
                        else Some (Orbiting (bodyId, startAngle, totalDegrees, radius, height, newElapsed, duration))
                    { state with Position = newPos; Target = bodyPos; ActiveMode = newMode }

        | Chasing (bodyId, offset) ->
            match Map.tryFind bodyId bodyPositions with
            | None -> state // body not yet in sim — hold position and wait
            | Some bodyPos ->
                let mutable bp = bodyPos
                let mutable off = offset
                let mutable newPos = Unchecked.defaultof<Vector3>
                Vector3.Add(&bp, &off, &newPos)
                { state with Position = newPos; Target = bodyPos }

        | Framing bodyIds ->
            let positions =
                bodyIds |> List.choose (fun id -> Map.tryFind id bodyPositions)
            if positions.IsEmpty then
                state // no bodies found yet — hold position and wait
            else
                let mutable minV = positions.Head
                let mutable maxV = positions.Head
                for p in positions.Tail do
                    minV <- Vector3(min minV.X p.X, min minV.Y p.Y, min minV.Z p.Z)
                    maxV <- Vector3(max maxV.X p.X, max maxV.Y p.Y, max maxV.Z p.Z)
                let center = lerpVec3 minV maxV 0.5f
                let mutable extent = Unchecked.defaultof<Vector3>
                let mutable mn = minV
                let mutable mx = maxV
                Vector3.Subtract(&mx, &mn, &extent)
                let size = max extent.X (max extent.Y extent.Z)
                let dist = max 5f (size * 1.5f)
                let camPos = Vector3(center.X + dist * 0.5f, center.Y + dist * 0.4f, center.Z + dist * 0.5f)
                { state with Position = camPos; Target = center }

        | Shaking (basePosition, baseTarget, intensity, elapsed, duration) ->
            let newElapsed = elapsed + dt
            if newElapsed >= duration then
                { state with Position = basePosition; Target = baseTarget; ActiveMode = None }
            else
                let ox = (float32 (rng.NextDouble()) - 0.5f) * 2f * intensity
                let oy = (float32 (rng.NextDouble()) - 0.5f) * 2f * intensity
                let oz = (float32 (rng.NextDouble()) - 0.5f) * 2f * intensity
                let shakeOffset = Vector3(ox, oy, oz)
                let mutable bp = basePosition
                let mutable so = shakeOffset
                let mutable newPos = Unchecked.defaultof<Vector3>
                Vector3.Add(&bp, &so, &newPos)
                { state with
                    Position = newPos
                    ActiveMode = Some (Shaking (basePosition, baseTarget, intensity, newElapsed, duration)) }

/// Returns true if a camera mode is currently active.
let isActive (state: CameraState) = state.ActiveMode.IsSome

/// Set the active camera mode.
let setMode (mode: CameraMode) (state: CameraState) =
    { state with ActiveMode = Some mode }
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
