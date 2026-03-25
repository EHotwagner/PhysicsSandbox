/// <summary>Commands for controlling the physics simulation: adding/removing bodies, applying forces, and managing playback.</summary>
module PhysicsClient.SimulationCommands

open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Session
open PhysicsClient.IdGenerator
open PhysicsClient.Vec3Helpers
open PhysicsClient.ShapeBuilders

/// <summary>Applies optional material, color, motion type, and collision parameters to an AddBody message.</summary>
let private applyBodyOptions
    (addBody: AddBody)
    (material: MaterialProperties option)
    (color: Color option)
    (motionType: BodyMotionType option)
    (collisionGroup: uint32 option)
    (collisionMask: uint32 option)
    =
    material |> Option.iter (fun m -> addBody.Material <- m)
    color |> Option.iter (fun c -> addBody.Color <- c)
    motionType |> Option.iter (fun mt -> addBody.MotionType <- mt)
    collisionGroup |> Option.iter (fun cg -> addBody.CollisionGroup <- cg)
    collisionMask |> Option.iter (fun cm -> addBody.CollisionMask <- cm)

/// <summary>Generic body creation: builds AddBody, sends command, registers in body registry.</summary>
let private addGenericBody
    (session: Session)
    (shapeKind: string)
    (shape: Shape)
    (position: float * float * float)
    (mass: float)
    (id: string option)
    (material: MaterialProperties option)
    (color: Color option)
    (motionType: BodyMotionType option)
    (collisionGroup: uint32 option)
    (collisionMask: uint32 option)
    : Result<string, string> =
    let bodyId = id |> Option.defaultWith (fun () -> nextId shapeKind)
    let addBody = AddBody()
    addBody.Id <- bodyId
    addBody.Position <- toVec3 position
    addBody.Mass <- mass
    addBody.Shape <- shape
    applyBodyOptions addBody material color motionType collisionGroup collisionMask
    let cmd = SimulationCommand()
    cmd.AddBody <- addBody
    match sendCommand session cmd with
    | Ok () ->
        if not ((bodyRegistry session).TryAdd(bodyId, shapeKind)) then
            Error $"Body '{bodyId}' already exists in registry"
        else
            Ok bodyId
    | Error e -> Error e

let addSphere (session: Session) (position: float * float * float) (radius: float) (mass: float) (id: string option) (material: MaterialProperties option) (color: Color option) (motionType: BodyMotionType option) (collisionGroup: uint32 option) (collisionMask: uint32 option) : Result<string, string> =
    addGenericBody session "sphere" (mkSphere radius) position mass id material color motionType collisionGroup collisionMask

let addBox (session: Session) (position: float * float * float) (halfExtents: float * float * float) (mass: float) (id: string option) (material: MaterialProperties option) (color: Color option) (motionType: BodyMotionType option) (collisionGroup: uint32 option) (collisionMask: uint32 option) : Result<string, string> =
    let (hx, hy, hz) = halfExtents
    addGenericBody session "box" (mkBox hx hy hz) position mass id material color motionType collisionGroup collisionMask

let addCapsule (session: Session) (position: float * float * float) (radius: float) (length: float) (mass: float) (id: string option) : Result<string, string> =
    addGenericBody session "capsule" (mkCapsule radius length) position mass id None None None None None

let addCylinder (session: Session) (position: float * float * float) (radius: float) (length: float) (mass: float) (id: string option) : Result<string, string> =
    addGenericBody session "cylinder" (mkCylinder radius length) position mass id None None None None None

/// <summary>Adds a constraint between two bodies in the simulation.</summary>
/// <param name="session">The active server session.</param>
/// <param name="id">Unique constraint identifier.</param>
/// <param name="bodyA">ID of the first body.</param>
/// <param name="bodyB">ID of the second body.</param>
/// <param name="constraintType">The constraint type configuration (ball-socket, hinge, weld, etc.).</param>
/// <returns>Ok with the constraint ID, or Error with a failure message.</returns>
let addConstraint (session: Session) (id: string) (bodyA: string) (bodyB: string) (constraintType: ConstraintType) : Result<string, string> =
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- constraintType
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    match sendCommand session cmd with
    | Ok () -> Ok id
    | Error e -> Error e

/// <summary>Removes a constraint from the simulation by its ID.</summary>
/// <param name="session">The active server session.</param>
/// <param name="constraintId">The ID of the constraint to remove.</param>
let removeConstraint (session: Session) (constraintId: string) : Result<unit, string> =
    let rc = RemoveConstraint()
    rc.ConstraintId <- constraintId
    let cmd = SimulationCommand()
    cmd.RemoveConstraint <- rc
    sendCommand session cmd

/// <summary>Registers a reusable shape with the simulation under a given handle.</summary>
/// <param name="session">The active server session.</param>
/// <param name="handle">Unique handle to identify the registered shape.</param>
/// <param name="shape">The shape definition to register.</param>
let registerShape (session: Session) (handle: string) (shape: Shape) : Result<unit, string> =
    let rs = RegisterShape()
    rs.ShapeHandle <- handle
    rs.Shape <- shape
    let cmd = SimulationCommand()
    cmd.RegisterShape <- rs
    sendCommand session cmd

/// <summary>Unregisters a previously registered shape by its handle.</summary>
/// <param name="session">The active server session.</param>
/// <param name="handle">The handle of the shape to unregister.</param>
let unregisterShape (session: Session) (handle: string) : Result<unit, string> =
    let us = UnregisterShape()
    us.ShapeHandle <- handle
    let cmd = SimulationCommand()
    cmd.UnregisterShape <- us
    sendCommand session cmd

/// <summary>Sets the collision filter for a body, controlling which groups it collides with.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the body to configure.</param>
/// <param name="collisionGroup">The collision group bitmask for this body.</param>
/// <param name="collisionMask">The collision mask bitmask (which groups this body collides with).</param>
let setCollisionFilter (session: Session) (bodyId: string) (collisionGroup: uint32) (collisionMask: uint32) : Result<unit, string> =
    let scf = SetCollisionFilter()
    scf.BodyId <- bodyId
    scf.CollisionGroup <- collisionGroup
    scf.CollisionMask <- collisionMask
    let cmd = SimulationCommand()
    cmd.SetCollisionFilter <- scf
    sendCommand session cmd

let addPlane (session: Session) (normal: (float * float * float) option) (id: string option) : Result<string, string> =
    let n = normal |> Option.defaultValue (0.0, 1.0, 0.0)
    addGenericBody session "plane" (mkPlane n) (0.0, 0.0, 0.0) 0.0 id None None None None None

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
        let mutable _removed = ""
        if not ((bodyRegistry session).TryRemove(bodyId, &_removed)) then
            Error $"Body '{bodyId}' not found in registry"
        else
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
    let mutable registryWarnings = 0
    for key in keys do
        let remove = RemoveBody()
        remove.BodyId <- key
        let cmd = SimulationCommand()
        cmd.RemoveBody <- remove
        match sendCommand session cmd with
        | Ok () ->
            let mutable _removed = ""
            if not (registry.TryRemove(key, &_removed)) then
                System.Diagnostics.Trace.TraceWarning($"SimulationCommands.clearAll: body '{key}' not found in registry during cleanup")
                registryWarnings <- registryWarnings + 1
        | Error e -> lastError <- Some e
    match lastError with
    | Some e -> Error e
    | None ->
        if registryWarnings > 0 then
            Ok count // still succeed, but warnings were counted internally
        else
            Ok count

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
    match sendCommand session cmd with
    | Ok () ->
        (bodyRegistry session).Clear()
        Ok ()
    | Error e -> Error e

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

/// <summary>Sets the position, orientation, and/or velocity of a body at runtime.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the body to update.</param>
/// <param name="position">New world-space position as (x, y, z).</param>
/// <param name="orientation">Optional new orientation quaternion as (x, y, z, w).</param>
/// <param name="velocity">Optional new linear velocity as (x, y, z).</param>
/// <param name="angularVelocity">Optional new angular velocity as (x, y, z).</param>
let setBodyPose
    (session: Session)
    (bodyId: string)
    (position: float * float * float)
    (orientation: (float * float * float * float) option)
    (velocity: (float * float * float) option)
    (angularVelocity: (float * float * float) option)
    : Result<unit, string> =
    let sbp = SetBodyPose()
    sbp.BodyId <- bodyId
    sbp.Position <- toVec3 position
    orientation |> Option.iter (fun (x, y, z, w) ->
        let q = Vec4()
        q.X <- x; q.Y <- y; q.Z <- z; q.W <- w
        sbp.Orientation <- q)
    velocity |> Option.iter (fun v -> sbp.Velocity <- toVec3 v)
    angularVelocity |> Option.iter (fun v -> sbp.AngularVelocity <- toVec3 v)
    let cmd = SimulationCommand()
    cmd.SetBodyPose <- sbp
    sendCommand session cmd

/// <summary>Casts a ray into the simulation and returns hit results.</summary>
/// <param name="session">The active server session.</param>
/// <param name="origin">Ray origin as (x, y, z).</param>
/// <param name="direction">Ray direction as (x, y, z).</param>
/// <param name="maxDistance">Maximum ray distance.</param>
/// <param name="allHits">If true, return all hits; otherwise, closest only.</param>
/// <param name="collisionMask">Optional collision mask filter.</param>
/// <returns>Ok with the raycast response, or Error with a failure message.</returns>
let raycast
    (session: Session)
    (origin: float * float * float)
    (direction: float * float * float)
    (maxDistance: float)
    (allHits: bool)
    (collisionMask: uint32 option)
    : Result<RaycastResponse, string> =
    if not (isConnected session) then
        Error "Not connected to server"
    else
        try
            let request = RaycastRequest()
            request.Origin <- toVec3 origin
            request.Direction <- toVec3 direction
            request.MaxDistance <- maxDistance
            request.AllHits <- allHits
            collisionMask |> Option.iter (fun cm -> request.CollisionMask <- cm)
            let response = (client session).Raycast(request)
            Ok response
        with
        | :? Grpc.Core.RpcException as ex ->
            Error $"gRPC error ({ex.StatusCode}): {ex.Status.Detail}"
        | ex ->
            Error $"Raycast failed: {ex.Message}"

/// <summary>Performs a sweep cast (shape cast) into the simulation.</summary>
/// <param name="session">The active server session.</param>
/// <param name="shape">The shape to sweep.</param>
/// <param name="startPosition">Starting position as (x, y, z).</param>
/// <param name="direction">Sweep direction as (x, y, z).</param>
/// <param name="maxDistance">Maximum sweep distance.</param>
/// <param name="orientation">Optional orientation quaternion as (x, y, z, w). Defaults to identity.</param>
/// <param name="collisionMask">Optional collision mask filter.</param>
/// <returns>Ok with the sweep cast response, or Error with a failure message.</returns>
let sweepCast
    (session: Session)
    (shape: Shape)
    (startPosition: float * float * float)
    (direction: float * float * float)
    (maxDistance: float)
    (orientation: (float * float * float * float) option)
    (collisionMask: uint32 option)
    : Result<SweepCastResponse, string> =
    if not (isConnected session) then
        Error "Not connected to server"
    else
        try
            let request = SweepCastRequest()
            request.Shape <- shape
            request.StartPosition <- toVec3 startPosition
            request.Direction <- toVec3 direction
            request.MaxDistance <- maxDistance
            orientation |> Option.iter (fun (x, y, z, w) ->
                let q = Vec4()
                q.X <- x
                q.Y <- y
                q.Z <- z
                q.W <- w
                request.Orientation <- q)
            collisionMask |> Option.iter (fun cm -> request.CollisionMask <- cm)
            let response = (client session).SweepCast(request)
            Ok response
        with
        | :? Grpc.Core.RpcException as ex ->
            Error $"gRPC error ({ex.StatusCode}): {ex.Status.Detail}"
        | ex ->
            Error $"SweepCast failed: {ex.Message}"

/// <summary>Tests for shape overlap at a given position.</summary>
/// <param name="session">The active server session.</param>
/// <param name="shape">The shape to test for overlap.</param>
/// <param name="position">Test position as (x, y, z).</param>
/// <param name="orientation">Optional orientation quaternion as (x, y, z, w). Defaults to identity.</param>
/// <param name="collisionMask">Optional collision mask filter.</param>
/// <returns>Ok with the overlap response containing overlapping body IDs, or Error with a failure message.</returns>
let overlap
    (session: Session)
    (shape: Shape)
    (position: float * float * float)
    (orientation: (float * float * float * float) option)
    (collisionMask: uint32 option)
    : Result<OverlapResponse, string> =
    if not (isConnected session) then
        Error "Not connected to server"
    else
        try
            let request = OverlapRequest()
            request.Shape <- shape
            request.Position <- toVec3 position
            orientation |> Option.iter (fun (x, y, z, w) ->
                let q = Vec4()
                q.X <- x
                q.Y <- y
                q.Z <- z
                q.W <- w
                request.Orientation <- q)
            collisionMask |> Option.iter (fun cm -> request.CollisionMask <- cm)
            let response = (client session).Overlap(request)
            Ok response
        with
        | :? Grpc.Core.RpcException as ex ->
            Error $"gRPC error ({ex.StatusCode}): {ex.Status.Detail}"
        | ex ->
            Error $"Overlap failed: {ex.Message}"

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
