/// <summary>MCP tool class exposing core simulation commands: adding/removing bodies, applying forces, stepping, and controlling playback.</summary>
module PhysicsSandbox.Mcp.SimulationTools

open System.ComponentModel
open System.Threading
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Mcp.GrpcConnection

let private counters = System.Collections.Concurrent.ConcurrentDictionary<string, int>()

/// <summary>Generates a unique body ID by appending an auto-incrementing counter to the shape name (e.g., "sphere" produces "sphere-1", "sphere-2", etc.).</summary>
/// <param name="shape">The shape name prefix.</param>
/// <returns>A unique identifier string.</returns>
let nextId (shape: string) =
    let value = counters.AddOrUpdate(shape, 1, fun _ current -> current + 1)
    $"{shape}-{value}"

let private sendCmd (conn: GrpcConnection) (cmd: SimulationCommand) =
    task {
        try
            let! ack = conn.Client.SendCommandAsync(cmd)
            if ack.Success then return $"Success: {ack.Message}"
            else return $"Failed: {ack.Message}"
        with ex ->
            return $"Error: {ex.Message}"
    }

/// <summary>MCP server tool type providing core simulation commands for adding bodies, applying forces, and controlling simulation playback.</summary>
[<McpServerToolType>]
type SimulationTools() =

    /// <summary>Adds a rigid body (sphere or box) to the physics simulation at the specified position with the given mass.</summary>
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
        let shapeLower = shape.ToLowerInvariant()
        let id = nextId shapeLower
        let body = AddBody(
            Id = id,
            Position = Vec3(X = px, Y = py, Z = pz),
            Mass = m
        )
        match shapeLower with
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

    /// <summary>Applies a continuous force vector to a body. The force persists across simulation steps until cleared.</summary>
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

    /// <summary>Applies an instantaneous impulse to a body, immediately changing its velocity by the given vector.</summary>
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

    /// <summary>Applies a rotational torque to a body around the specified axis vector.</summary>
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

    /// <summary>Sets the global gravity vector for the simulation. Defaults to Earth gravity (0, -9.81, 0) when components are omitted.</summary>
    [<McpServerTool; Description("Set the global gravity vector.")>]
    static member set_gravity(
        conn: GrpcConnection,
        [<Description("Gravity X")>] ?x: float,
        [<Description("Gravity Y")>] ?y: float,
        [<Description("Gravity Z")>] ?z: float
    ) : Task<string> =
        let cmd = SetGravity(Gravity = Vec3(X = defaultArg x 0.0, Y = defaultArg y -9.81, Z = defaultArg z 0.0))
        sendCmd conn (SimulationCommand(SetGravity = cmd))

    /// <summary>Advances the physics simulation by a single time step.</summary>
    [<McpServerTool; Description("Advance the simulation by one time step.")>]
    static member step(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(Step = StepSimulation()))

    /// <summary>Starts continuous simulation playback, stepping automatically each frame.</summary>
    [<McpServerTool; Description("Start continuous simulation.")>]
    static member play(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(PlayPause = PlayPause(Running = true)))

    /// <summary>Pauses continuous simulation playback, freezing all bodies in place.</summary>
    [<McpServerTool; Description("Pause the simulation.")>]
    static member pause(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(PlayPause = PlayPause(Running = false)))

    /// <summary>Removes a body from the simulation by its identifier.</summary>
    [<McpServerTool; Description("Remove a body from the simulation.")>]
    static member remove_body(
        conn: GrpcConnection,
        [<Description("Body ID to remove")>] body_id: string
    ) : Task<string> =
        sendCmd conn (SimulationCommand(RemoveBody = RemoveBody(BodyId = body_id)))

    /// <summary>Clears all accumulated continuous forces on a body.</summary>
    [<McpServerTool; Description("Clear all forces on a body.")>]
    static member clear_forces(
        conn: GrpcConnection,
        [<Description("Body ID")>] body_id: string
    ) : Task<string> =
        sendCmd conn (SimulationCommand(ClearForces = ClearForces(BodyId = body_id)))

    /// <summary>Resets the entire simulation: removes all bodies, clears forces, and resets time to zero. Performance metrics persist across restarts.</summary>
    [<McpServerTool; Description("Reset the simulation: remove all bodies, clear forces, reset time to 0. Performance metrics persist across restarts.")>]
    static member restart_simulation(conn: GrpcConnection) : Task<string> =
        sendCmd conn (SimulationCommand(Reset = ResetSimulation()))
