/// <summary>High-level body movement commands that translate human-friendly directions and targets into physics impulses and torques.</summary>
module PhysicsClient.Steering

open PhysicsClient.Session
open PhysicsClient.SimulationCommands

/// <summary>Represents a named spatial direction for intuitive body steering.</summary>
type Direction =
    /// <summary>Positive Y axis (upward).</summary>
    | Up
    /// <summary>Negative Y axis (downward).</summary>
    | Down
    /// <summary>Negative Z axis (forward in right-handed coordinates).</summary>
    | North
    /// <summary>Positive Z axis (backward in right-handed coordinates).</summary>
    | South
    /// <summary>Positive X axis (rightward).</summary>
    | East
    /// <summary>Negative X axis (leftward).</summary>
    | West

/// <summary>Converts a Direction to its corresponding unit vector as (x, y, z).</summary>
let directionToVec (d: Direction) =
    match d with
    | Up -> (0.0, 1.0, 0.0)
    | Down -> (0.0, -1.0, 0.0)
    | North -> (0.0, 0.0, -1.0)
    | South -> (0.0, 0.0, 1.0)
    | East -> (1.0, 0.0, 0.0)
    | West -> (-1.0, 0.0, 0.0)

/// <summary>Applies an impulse to a body in the specified compass direction, scaled by magnitude.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the target body.</param>
/// <param name="direction">The direction to push the body.</param>
/// <param name="magnitude">The strength of the impulse in Newton-seconds.</param>
let push session bodyId direction magnitude =
    let (dx, dy, dz) = directionToVec direction
    applyImpulse session bodyId (dx * magnitude, dy * magnitude, dz * magnitude)

/// <summary>Applies an impulse to a body using an arbitrary vector.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the target body.</param>
/// <param name="vector">The impulse vector as (x, y, z) in Newton-seconds.</param>
let pushVec session bodyId vector =
    applyImpulse session bodyId vector

/// <summary>Launches a body toward a target position by computing the direction from the body's current location and applying an impulse of the given speed.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the body to launch.</param>
/// <param name="target">The world-space target position as (x, y, z).</param>
/// <param name="speed">The impulse magnitude in Newton-seconds.</param>
let launch session bodyId (target: float * float * float) speed =
    match latestState session with
    | None -> Error "No simulation state available"
    | Some state ->
        let body = state.Bodies |> Seq.tryFind (fun b -> b.Id = bodyId)
        match body with
        | None -> Error $"Body '{bodyId}' not found in state"
        | Some b ->
            let (tx, ty, tz) = target
            let dx = tx - b.Position.X
            let dy = ty - b.Position.Y
            let dz = tz - b.Position.Z
            let len = sqrt(dx*dx + dy*dy + dz*dz)
            if len < 0.001 then Error "Target is at body position"
            else
                let nx, ny, nz = dx/len, dy/len, dz/len
                applyImpulse session bodyId (nx * speed, ny * speed, nz * speed)

/// <summary>Applies a torque to spin a body around the specified axis direction.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the target body.</param>
/// <param name="axis">The rotation axis as a Direction.</param>
/// <param name="magnitude">The torque strength in Newton-meters.</param>
let spin session bodyId axis magnitude =
    let (ax, ay, az) = directionToVec axis
    applyTorque session bodyId (ax * magnitude, ay * magnitude, az * magnitude)

/// <summary>Attempts to stop a body by clearing its forces and applying a counter-impulse to cancel its current velocity.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the body to stop.</param>
let stop session bodyId =
    match clearForces session bodyId with
    | Error e -> Error e
    | Ok () ->
        match latestState session with
        | None -> Ok () // No state, just cleared forces
        | Some state ->
            let body = state.Bodies |> Seq.tryFind (fun b -> b.Id = bodyId)
            match body with
            | None -> Ok ()
            | Some b ->
                let vx, vy, vz = b.Velocity.X, b.Velocity.Y, b.Velocity.Z
                let mass = b.Mass
                if mass > 0.0 && (abs vx > 0.001 || abs vy > 0.001 || abs vz > 0.001) then
                    applyImpulse session bodyId (-vx * mass, -vy * mass, -vz * mass)
                else
                    Ok ()
