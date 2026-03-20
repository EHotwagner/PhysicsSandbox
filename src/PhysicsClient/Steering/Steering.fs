module PhysicsClient.Steering

open PhysicsClient.Session
open PhysicsClient.SimulationCommands

type Direction = Up | Down | North | South | East | West

let directionToVec (d: Direction) =
    match d with
    | Up -> (0.0, 1.0, 0.0)
    | Down -> (0.0, -1.0, 0.0)
    | North -> (0.0, 0.0, -1.0)
    | South -> (0.0, 0.0, 1.0)
    | East -> (1.0, 0.0, 0.0)
    | West -> (-1.0, 0.0, 0.0)

let push session bodyId direction magnitude =
    let (dx, dy, dz) = directionToVec direction
    applyImpulse session bodyId (dx * magnitude, dy * magnitude, dz * magnitude)

let pushVec session bodyId vector =
    applyImpulse session bodyId vector

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

let spin session bodyId axis magnitude =
    let (ax, ay, az) = directionToVec axis
    applyTorque session bodyId (ax * magnitude, ay * magnitude, az * magnitude)

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
