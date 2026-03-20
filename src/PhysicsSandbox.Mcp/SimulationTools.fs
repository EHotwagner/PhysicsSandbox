module PhysicsSandbox.Mcp.SimulationTools

open System.ComponentModel
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Mcp.GrpcConnection

let private sendCmd (conn: GrpcConnection) (cmd: SimulationCommand) =
    task {
        try
            let! ack = conn.Client.SendCommandAsync(cmd)
            if ack.Success then return $"Success: {ack.Message}"
            else return $"Failed: {ack.Message}"
        with ex ->
            return $"Error: {ex.Message}"
    }

[<McpServerToolType>]
type SimulationTools() =

    [<McpServerTool; Description("Add a rigid body to the physics simulation.")>]
    static member add_body(
        conn: GrpcConnection,
        [<Description("Body shape: 'sphere' or 'box'")>] shape: string,
        [<Description("Sphere radius (required if shape=sphere)")>] ?radius: float,
        [<Description("Box half-extent X (required if shape=box)")>] ?half_extents_x: float,
        [<Description("Box half-extent Y (required if shape=box)")>] ?half_extents_y: float,
        [<Description("Box half-extent Z (required if shape=box)")>] ?half_extents_z: float,
        [<Description("Position X")>] ?x: float,
        [<Description("Position Y")>] ?y: float,
        [<Description("Position Z")>] ?z: float,
        [<Description("Body mass (0 = static)")>] ?mass: float
    ) : Task<string> =
        let px = defaultArg x 0.0
        let py = defaultArg y 5.0
        let pz = defaultArg z 0.0
        let m = defaultArg mass 1.0
        let body = AddBody(
            Position = Vec3(X = px, Y = py, Z = pz),
            Mass = m
        )
        match shape.ToLowerInvariant() with
        | "sphere" ->
            let r = defaultArg radius 0.5
            body.Shape <- Shape(Sphere = Sphere(Radius = r))
        | "box" ->
            let hx = defaultArg half_extents_x 0.5
            let hy = defaultArg half_extents_y 0.5
            let hz = defaultArg half_extents_z 0.5
            body.Shape <- Shape(Box = Box(HalfExtents = Vec3(X = hx, Y = hy, Z = hz)))
        | _ -> ()
        sendCmd conn (SimulationCommand(AddBody = body))

    [<McpServerTool; Description("Apply a continuous force to a body.")>]
    static member apply_force(
        conn: GrpcConnection,
        [<Description("Target body ID")>] body_id: string,
        [<Description("Force X component")>] ?x: float,
        [<Description("Force Y component")>] ?y: float,
        [<Description("Force Z component")>] ?z: float
    ) : Task<string> =
        let cmd = ApplyForce(BodyId = body_id, Force = Vec3(X = defaultArg x 0.0, Y = defaultArg y 0.0, Z = defaultArg z 0.0))
        sendCmd conn (SimulationCommand(ApplyForce = cmd))

    [<McpServerTool; Description("Apply an instantaneous impulse to a body.")>]
    static member apply_impulse(
        conn: GrpcConnection,
        [<Description("Target body ID")>] body_id: string,
        [<Description("Impulse X component")>] ?x: float,
        [<Description("Impulse Y component")>] ?y: float,
        [<Description("Impulse Z component")>] ?z: float
    ) : Task<string> =
        let cmd = ApplyImpulse(BodyId = body_id, Impulse = Vec3(X = defaultArg x 0.0, Y = defaultArg y 0.0, Z = defaultArg z 0.0))
        sendCmd conn (SimulationCommand(ApplyImpulse = cmd))

    [<McpServerTool; Description("Apply a torque to a body.")>]
    static member apply_torque(
        conn: GrpcConnection,
        [<Description("Target body ID")>] body_id: string,
        [<Description("Torque X component")>] ?x: float,
        [<Description("Torque Y component")>] ?y: float,
        [<Description("Torque Z component")>] ?z: float
    ) : Task<string> =
        let cmd = ApplyTorque(BodyId = body_id, Torque = Vec3(X = defaultArg x 0.0, Y = defaultArg y 0.0, Z = defaultArg z 0.0))
        sendCmd conn (SimulationCommand(ApplyTorque = cmd))

    [<McpServerTool; Description("Set the global gravity vector.")>]
    static member set_gravity(
        conn: GrpcConnection,
        [<Description("Gravity X")>] ?x: float,
        [<Description("Gravity Y")>] ?y: float,
        [<Description("Gravity Z")>] ?z: float
    ) : Task<string> =
        let cmd = SetGravity(Gravity = Vec3(X = defaultArg x 0.0, Y = defaultArg y -9.81, Z = defaultArg z 0.0))
        sendCmd conn (SimulationCommand(SetGravity = cmd))

    [<McpServerTool; Description("Advance the simulation by one time step.")>]
    static member step(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(Step = StepSimulation()))

    [<McpServerTool; Description("Start continuous simulation.")>]
    static member play(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(PlayPause = PlayPause(Running = true)))

    [<McpServerTool; Description("Pause the simulation.")>]
    static member pause(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(PlayPause = PlayPause(Running = false)))

    [<McpServerTool; Description("Remove a body from the simulation.")>]
    static member remove_body(
        conn: GrpcConnection,
        [<Description("Body ID to remove")>] body_id: string
    ) : Task<string> =
        sendCmd conn (SimulationCommand(RemoveBody = RemoveBody(BodyId = body_id)))

    [<McpServerTool; Description("Clear all forces on a body.")>]
    static member clear_forces(
        conn: GrpcConnection,
        [<Description("Body ID")>] body_id: string
    ) : Task<string> =
        sendCmd conn (SimulationCommand(ClearForces = ClearForces(BodyId = body_id)))
