/// <summary>Adapter layer that translates high-level body operations into gRPC SimulationCommand messages.</summary>
module PhysicsSandbox.Mcp.ClientAdapter

open PhysicsSandbox.Mcp.GrpcConnection
open PhysicsSandbox.Shared.Contracts

/// <summary>Creates a protobuf Vec3 from a float triple.</summary>
/// <param name="x">X component.</param>
/// <param name="y">Y component.</param>
/// <param name="z">Z component.</param>
/// <returns>A new Vec3 instance with the given coordinates.</returns>
let toVec3 (x: float, y: float, z: float) =
    let v = Vec3()
    v.X <- x
    v.Y <- y
    v.Z <- z
    v

/// <summary>Sends a simulation command via the shared GrpcConnection and returns the server acknowledgment message.</summary>
/// <param name="connection">The active gRPC connection to the physics server.</param>
/// <param name="cmd">The simulation command to send.</param>
/// <returns>The server's acknowledgment message, or an error string on failure.</returns>
let sendCommand (connection: GrpcConnection) (cmd: SimulationCommand) =
    try
        let ack = connection.Client.SendCommand(cmd)
        if ack.Success then ack.Message
        else $"Error: {ack.Message}"
    with ex ->
        $"Error: {ex.Message}"

/// <summary>Adds a sphere body to the simulation at the given position.</summary>
/// <param name="connection">The active gRPC connection.</param>
/// <param name="position">World-space position as (x, y, z).</param>
/// <param name="radius">Sphere radius.</param>
/// <param name="mass">Body mass (0 for static).</param>
/// <param name="id">Unique body identifier.</param>
/// <returns>The server's acknowledgment message.</returns>
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

/// <summary>Adds a box body to the simulation at the given position.</summary>
/// <param name="connection">The active gRPC connection.</param>
/// <param name="position">World-space position as (x, y, z).</param>
/// <param name="halfExtents">Box half-extents as (hx, hy, hz).</param>
/// <param name="mass">Body mass (0 for static).</param>
/// <param name="id">Unique body identifier.</param>
/// <returns>The server's acknowledgment message.</returns>
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

/// <summary>Applies an instantaneous impulse to a body, changing its velocity immediately.</summary>
/// <param name="connection">The active gRPC connection.</param>
/// <param name="bodyId">Target body identifier.</param>
/// <param name="impulse">Impulse vector as (x, y, z).</param>
/// <returns>The server's acknowledgment message.</returns>
let applyImpulse (connection: GrpcConnection) (bodyId: string) (impulse: float * float * float) =
    let ai = ApplyImpulse()
    ai.BodyId <- bodyId
    ai.Impulse <- toVec3 impulse
    let cmd = SimulationCommand()
    cmd.ApplyImpulse <- ai
    sendCommand connection cmd

/// <summary>Applies a rotational torque to a body around the given axis.</summary>
/// <param name="connection">The active gRPC connection.</param>
/// <param name="bodyId">Target body identifier.</param>
/// <param name="torque">Torque vector as (x, y, z).</param>
/// <returns>The server's acknowledgment message.</returns>
let applyTorque (connection: GrpcConnection) (bodyId: string) (torque: float * float * float) =
    let at = ApplyTorque()
    at.BodyId <- bodyId
    at.Torque <- toVec3 torque
    let cmd = SimulationCommand()
    cmd.ApplyTorque <- at
    sendCommand connection cmd

/// <summary>Clears all accumulated forces on a body, stopping any continuous force application.</summary>
/// <param name="connection">The active gRPC connection.</param>
/// <param name="bodyId">Target body identifier.</param>
/// <returns>The server's acknowledgment message.</returns>
let clearForces (connection: GrpcConnection) (bodyId: string) =
    let cf = ClearForces()
    cf.BodyId <- bodyId
    let cmd = SimulationCommand()
    cmd.ClearForces <- cf
    sendCommand connection cmd
