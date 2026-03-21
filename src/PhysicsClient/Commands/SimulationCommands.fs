/// <summary>Commands for controlling the physics simulation: adding/removing bodies, applying forces, and managing playback.</summary>
module PhysicsClient.SimulationCommands

open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Session
open PhysicsClient.IdGenerator

/// <summary>Converts an F# float triple to a protobuf Vec3 message.</summary>
let internal toVec3 (x: float, y: float, z: float) =
    let v = Vec3()
    v.X <- x
    v.Y <- y
    v.Z <- z
    v

/// <summary>Adds a sphere body to the simulation and registers it in the local body registry.</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">World-space position as (x, y, z).</param>
/// <param name="radius">Sphere radius in meters.</param>
/// <param name="mass">Body mass in kilograms.</param>
/// <param name="id">Optional custom body ID; auto-generated if None.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let addSphere (session: Session) (position: float * float * float) (radius: float) (mass: float) (id: string option) : Result<string, string> =
    let bodyId = id |> Option.defaultWith (fun () -> nextId "sphere")
    let sphere = Sphere()
    sphere.Radius <- radius
    let shape = Shape()
    shape.Sphere <- sphere
    let addBody = AddBody()
    addBody.Id <- bodyId
    addBody.Position <- toVec3 position
    addBody.Mass <- mass
    addBody.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- addBody
    match sendCommand session cmd with
    | Ok () ->
        (bodyRegistry session).TryAdd(bodyId, "sphere") |> ignore
        Ok bodyId
    | Error e -> Error e

/// <summary>Adds a box body to the simulation and registers it in the local body registry.</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">World-space position as (x, y, z).</param>
/// <param name="halfExtents">Half-extents of the box as (hx, hy, hz).</param>
/// <param name="mass">Body mass in kilograms.</param>
/// <param name="id">Optional custom body ID; auto-generated if None.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let addBox (session: Session) (position: float * float * float) (halfExtents: float * float * float) (mass: float) (id: string option) : Result<string, string> =
    let bodyId = id |> Option.defaultWith (fun () -> nextId "box")
    let box = Box()
    box.HalfExtents <- toVec3 halfExtents
    let shape = Shape()
    shape.Box <- box
    let addBody = AddBody()
    addBody.Id <- bodyId
    addBody.Position <- toVec3 position
    addBody.Mass <- mass
    addBody.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- addBody
    match sendCommand session cmd with
    | Ok () ->
        (bodyRegistry session).TryAdd(bodyId, "box") |> ignore
        Ok bodyId
    | Error e -> Error e

/// <summary>Adds a static ground plane to the simulation. Planes are approximated as large static boxes.</summary>
/// <param name="session">The active server session.</param>
/// <param name="normal">Plane normal direction; defaults to (0, 1, 0) (upward).</param>
/// <param name="id">Optional custom body ID; auto-generated if None.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let addPlane (session: Session) (normal: (float * float * float) option) (id: string option) : Result<string, string> =
    let bodyId = id |> Option.defaultWith (fun () -> nextId "plane")
    let n = normal |> Option.defaultValue (0.0, 1.0, 0.0)
    let plane = Plane()
    plane.Normal <- toVec3 n
    let shape = Shape()
    shape.Plane <- plane
    let addBody = AddBody()
    addBody.Id <- bodyId
    addBody.Position <- toVec3 (0.0, 0.0, 0.0)
    addBody.Mass <- 0.0
    addBody.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- addBody
    match sendCommand session cmd with
    | Ok () ->
        (bodyRegistry session).TryAdd(bodyId, "plane") |> ignore
        Ok bodyId
    | Error e -> Error e

/// <summary>Removes a body from the simulation by its ID and unregisters it locally.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the body to remove.</param>
let removeBody (session: Session) (bodyId: string) : Result<unit, string> =
    let remove = RemoveBody()
    remove.BodyId <- bodyId
    let cmd = SimulationCommand()
    cmd.RemoveBody <- remove
    match sendCommand session cmd with
    | Ok () ->
        (bodyRegistry session).TryRemove(bodyId) |> ignore
        Ok ()
    | Error e -> Error e

/// <summary>Removes all registered bodies from the simulation one by one.</summary>
/// <param name="session">The active server session.</param>
/// <returns>Ok with the number of bodies removed, or Error if any removal fails.</returns>
let clearAll (session: Session) : Result<int, string> =
    let registry = bodyRegistry session
    let keys = registry.Keys |> Seq.toList
    let count = keys.Length
    let mutable lastError = None
    for key in keys do
        let remove = RemoveBody()
        remove.BodyId <- key
        let cmd = SimulationCommand()
        cmd.RemoveBody <- remove
        match sendCommand session cmd with
        | Ok () -> registry.TryRemove(key) |> ignore
        | Error e -> lastError <- Some e
    match lastError with
    | Some e -> Error e
    | None -> Ok count

/// <summary>Applies a continuous force to a body. The force persists until cleared.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the target body.</param>
/// <param name="force">Force vector as (x, y, z) in Newtons.</param>
let applyForce (session: Session) (bodyId: string) (force: float * float * float) : Result<unit, string> =
    let af = ApplyForce()
    af.BodyId <- bodyId
    af.Force <- toVec3 force
    let cmd = SimulationCommand()
    cmd.ApplyForce <- af
    sendCommand session cmd

/// <summary>Applies an instantaneous impulse to a body, immediately changing its velocity.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the target body.</param>
/// <param name="impulse">Impulse vector as (x, y, z) in Newton-seconds.</param>
let applyImpulse (session: Session) (bodyId: string) (impulse: float * float * float) : Result<unit, string> =
    let ai = ApplyImpulse()
    ai.BodyId <- bodyId
    ai.Impulse <- toVec3 impulse
    let cmd = SimulationCommand()
    cmd.ApplyImpulse <- ai
    sendCommand session cmd

/// <summary>Applies a torque to a body, causing angular acceleration around the specified axis.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the target body.</param>
/// <param name="torque">Torque vector as (x, y, z) in Newton-meters.</param>
let applyTorque (session: Session) (bodyId: string) (torque: float * float * float) : Result<unit, string> =
    let at = ApplyTorque()
    at.BodyId <- bodyId
    at.Torque <- toVec3 torque
    let cmd = SimulationCommand()
    cmd.ApplyTorque <- at
    sendCommand session cmd

/// <summary>Clears all accumulated forces on a body, stopping any continuous force application.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the target body.</param>
let clearForces (session: Session) (bodyId: string) : Result<unit, string> =
    let cf = ClearForces()
    cf.BodyId <- bodyId
    let cmd = SimulationCommand()
    cmd.ClearForces <- cf
    sendCommand session cmd

/// <summary>Sets the global gravity vector for the simulation.</summary>
/// <param name="session">The active server session.</param>
/// <param name="gravity">Gravity vector as (x, y, z) in m/s^2. Earth gravity is (0, -9.81, 0).</param>
let setGravity (session: Session) (gravity: float * float * float) : Result<unit, string> =
    let sg = SetGravity()
    sg.Gravity <- toVec3 gravity
    let cmd = SimulationCommand()
    cmd.SetGravity <- sg
    sendCommand session cmd

/// <summary>Resumes the simulation, allowing physics to advance each frame.</summary>
let play (session: Session) : Result<unit, string> =
    let pp = PlayPause()
    pp.Running <- true
    let cmd = SimulationCommand()
    cmd.PlayPause <- pp
    sendCommand session cmd

/// <summary>Pauses the simulation, freezing all bodies in place.</summary>
let pause (session: Session) : Result<unit, string> =
    let pp = PlayPause()
    pp.Running <- false
    let cmd = SimulationCommand()
    cmd.PlayPause <- pp
    sendCommand session cmd

/// <summary>Advances the simulation by a single time step while paused.</summary>
let step (session: Session) : Result<unit, string> =
    let s = StepSimulation()
    s.DeltaTime <- 0.0
    let cmd = SimulationCommand()
    cmd.Step <- s
    sendCommand session cmd

/// <summary>Resets the simulation to its initial state, removing all bodies and resetting the clock.</summary>
let reset (session: Session) : Result<unit, string> =
    let cmd = SimulationCommand()
    cmd.Reset <- ResetSimulation()
    sendCommand session cmd

/// <summary>Sends multiple simulation commands in a single batch request for better performance.</summary>
/// <param name="session">The active server session.</param>
/// <param name="commands">List of simulation commands to execute atomically.</param>
/// <returns>Ok with the batch response containing per-command results, or Error on transport failure.</returns>
let batchCommands (session: Session) (commands: SimulationCommand list) : Result<BatchResponse, string> =
    try
        let batch = BatchSimulationRequest()
        for cmd in commands do
            batch.Commands.Add(cmd)
        let response = (client session).SendBatchCommand(batch)
        Ok response
    with ex ->
        Error $"Batch command failed: {ex.Message}"

/// <summary>Sends multiple view commands in a single batch request for better performance.</summary>
/// <param name="session">The active server session.</param>
/// <param name="commands">List of view commands to execute atomically.</param>
/// <returns>Ok with the batch response containing per-command results, or Error on transport failure.</returns>
let batchViewCommands (session: Session) (commands: ViewCommand list) : Result<BatchResponse, string> =
    try
        let batch = BatchViewRequest()
        for cmd in commands do
            batch.Commands.Add(cmd)
        let response = (client session).SendBatchViewCommand(batch)
        Ok response
    with ex ->
        Error $"Batch view command failed: {ex.Message}"
