module PhysicsClient.SimulationCommands

open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Session
open PhysicsClient.IdGenerator

let internal toVec3 (x: float, y: float, z: float) =
    let v = Vec3()
    v.X <- x
    v.Y <- y
    v.Z <- z
    v

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

let applyForce (session: Session) (bodyId: string) (force: float * float * float) : Result<unit, string> =
    let af = ApplyForce()
    af.BodyId <- bodyId
    af.Force <- toVec3 force
    let cmd = SimulationCommand()
    cmd.ApplyForce <- af
    sendCommand session cmd

let applyImpulse (session: Session) (bodyId: string) (impulse: float * float * float) : Result<unit, string> =
    let ai = ApplyImpulse()
    ai.BodyId <- bodyId
    ai.Impulse <- toVec3 impulse
    let cmd = SimulationCommand()
    cmd.ApplyImpulse <- ai
    sendCommand session cmd

let applyTorque (session: Session) (bodyId: string) (torque: float * float * float) : Result<unit, string> =
    let at = ApplyTorque()
    at.BodyId <- bodyId
    at.Torque <- toVec3 torque
    let cmd = SimulationCommand()
    cmd.ApplyTorque <- at
    sendCommand session cmd

let clearForces (session: Session) (bodyId: string) : Result<unit, string> =
    let cf = ClearForces()
    cf.BodyId <- bodyId
    let cmd = SimulationCommand()
    cmd.ClearForces <- cf
    sendCommand session cmd

let setGravity (session: Session) (gravity: float * float * float) : Result<unit, string> =
    let sg = SetGravity()
    sg.Gravity <- toVec3 gravity
    let cmd = SimulationCommand()
    cmd.SetGravity <- sg
    sendCommand session cmd

let play (session: Session) : Result<unit, string> =
    let pp = PlayPause()
    pp.Running <- true
    let cmd = SimulationCommand()
    cmd.PlayPause <- pp
    sendCommand session cmd

let pause (session: Session) : Result<unit, string> =
    let pp = PlayPause()
    pp.Running <- false
    let cmd = SimulationCommand()
    cmd.PlayPause <- pp
    sendCommand session cmd

let step (session: Session) : Result<unit, string> =
    let s = StepSimulation()
    s.DeltaTime <- 0.0
    let cmd = SimulationCommand()
    cmd.Step <- s
    sendCommand session cmd

let reset (session: Session) : Result<unit, string> =
    let cmd = SimulationCommand()
    cmd.Reset <- ResetSimulation()
    sendCommand session cmd

let batchCommands (session: Session) (commands: SimulationCommand list) : Result<BatchResponse, string> =
    try
        let batch = BatchSimulationRequest()
        for cmd in commands do
            batch.Commands.Add(cmd)
        let response = (client session).SendBatchCommand(batch)
        Ok response
    with ex ->
        Error $"Batch command failed: {ex.Message}"

let batchViewCommands (session: Session) (commands: ViewCommand list) : Result<BatchResponse, string> =
    try
        let batch = BatchViewRequest()
        for cmd in commands do
            batch.Commands.Add(cmd)
        let response = (client session).SendBatchViewCommand(batch)
        Ok response
    with ex ->
        Error $"Batch view command failed: {ex.Message}"
