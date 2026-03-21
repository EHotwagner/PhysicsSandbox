module PhysicsSandbox.Mcp.ClientAdapter

open PhysicsSandbox.Mcp.GrpcConnection
open PhysicsSandbox.Shared.Contracts

let toVec3 (x: float, y: float, z: float) =
    let v = Vec3()
    v.X <- x
    v.Y <- y
    v.Z <- z
    v

let sendCommand (connection: GrpcConnection) (cmd: SimulationCommand) =
    try
        let ack = connection.Client.SendCommand(cmd)
        if ack.Success then ack.Message
        else $"Error: {ack.Message}"
    with ex ->
        $"Error: {ex.Message}"

let addSphere (connection: GrpcConnection) (position: float * float * float) (radius: float) (mass: float) (id: string) =
    let sphere = Sphere()
    sphere.Radius <- radius
    let shape = Shape()
    shape.Sphere <- sphere
    let addBody = AddBody()
    addBody.Id <- id
    addBody.Position <- toVec3 position
    addBody.Mass <- mass
    addBody.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- addBody
    sendCommand connection cmd

let addBox (connection: GrpcConnection) (position: float * float * float) (halfExtents: float * float * float) (mass: float) (id: string) =
    let box = Box()
    box.HalfExtents <- toVec3 halfExtents
    let shape = Shape()
    shape.Box <- box
    let addBody = AddBody()
    addBody.Id <- id
    addBody.Position <- toVec3 position
    addBody.Mass <- mass
    addBody.Shape <- shape
    let cmd = SimulationCommand()
    cmd.AddBody <- addBody
    sendCommand connection cmd

let applyImpulse (connection: GrpcConnection) (bodyId: string) (impulse: float * float * float) =
    let ai = ApplyImpulse()
    ai.BodyId <- bodyId
    ai.Impulse <- toVec3 impulse
    let cmd = SimulationCommand()
    cmd.ApplyImpulse <- ai
    sendCommand connection cmd

let applyTorque (connection: GrpcConnection) (bodyId: string) (torque: float * float * float) =
    let at = ApplyTorque()
    at.BodyId <- bodyId
    at.Torque <- toVec3 torque
    let cmd = SimulationCommand()
    cmd.ApplyTorque <- at
    sendCommand connection cmd

let clearForces (connection: GrpcConnection) (bodyId: string) =
    let cf = ClearForces()
    cf.BodyId <- bodyId
    let cmd = SimulationCommand()
    cmd.ClearForces <- cf
    sendCommand connection cmd
