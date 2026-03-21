/// <summary>MCP tool class for high-level body movement: pushing in compass directions, launching toward targets, spinning, and stopping.</summary>
module PhysicsSandbox.Mcp.SteeringTools

open System.ComponentModel
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection
open PhysicsSandbox.Mcp.ClientAdapter

let private directionToVec (direction: string) =
    match direction.ToLowerInvariant() with
    | "up" -> Some (0.0, 1.0, 0.0)
    | "down" -> Some (0.0, -1.0, 0.0)
    | "north" -> Some (0.0, 0.0, -1.0)
    | "south" -> Some (0.0, 0.0, 1.0)
    | "east" -> Some (1.0, 0.0, 0.0)
    | "west" -> Some (-1.0, 0.0, 0.0)
    | _ -> None

/// <summary>MCP server tool type providing intuitive body movement commands using compass directions and target-based launching.</summary>
[<McpServerToolType>]
type SteeringTools() =

    /// <summary>Pushes a body with an impulse in one of six compass directions (up, down, north, south, east, west).</summary>
    [<McpServerTool>]
    [<Description("Push a body in a compass direction (up/down/north/south/east/west)")>]
    static member push_body(connection: GrpcConnection,
                            [<Description("Body ID to push")>] body_id: string,
                            [<Description("Direction: up, down, north, south, east, west")>] direction: string,
                            [<Description("Impulse magnitude (default 10)")>] strength: float) =
        match directionToVec direction with
        | None -> $"Error: Unknown direction '{direction}'. Use: up, down, north, south, east, west"
        | Some (dx, dy, dz) ->
            let mag = if strength <= 0.0 then 10.0 else strength
            applyImpulse connection body_id (dx * mag, dy * mag, dz * mag)

    /// <summary>Launches a body toward a target position by computing the direction vector from the body's current position and applying a normalized impulse at the given speed.</summary>
    [<McpServerTool>]
    [<Description("Launch a body toward a target position")>]
    static member launch_body(connection: GrpcConnection,
                              [<Description("Body ID to launch")>] body_id: string,
                              [<Description("Target X")>] target_x: float,
                              [<Description("Target Y")>] target_y: float,
                              [<Description("Target Z")>] target_z: float,
                              [<Description("Launch speed (default 10)")>] speed: float) =
        match connection.LatestState with
        | None -> "Error: No simulation state available"
        | Some state ->
            let body = state.Bodies |> Seq.tryFind (fun b -> b.Id = body_id)
            match body with
            | None -> $"Error: Body '{body_id}' not found in state"
            | Some b ->
                let dx = target_x - b.Position.X
                let dy = target_y - b.Position.Y
                let dz = target_z - b.Position.Z
                let len = sqrt(dx*dx + dy*dy + dz*dz)
                if len < 0.001 then "Error: Target is at body position"
                else
                    let sp = if speed <= 0.0 then 10.0 else speed
                    let nx, ny, nz = dx/len, dy/len, dz/len
                    applyImpulse connection body_id (nx * sp, ny * sp, nz * sp)

    /// <summary>Applies a rotational torque to spin a body around one of six compass-direction axes.</summary>
    [<McpServerTool>]
    [<Description("Spin a body around an axis")>]
    static member spin_body(connection: GrpcConnection,
                            [<Description("Body ID to spin")>] body_id: string,
                            [<Description("Axis direction: up, down, north, south, east, west")>] axis: string,
                            [<Description("Torque magnitude (default 10)")>] strength: float) =
        match directionToVec axis with
        | None -> $"Error: Unknown axis '{axis}'. Use: up, down, north, south, east, west"
        | Some (ax, ay, az) ->
            let mag = if strength <= 0.0 then 10.0 else strength
            applyTorque connection body_id (ax * mag, ay * mag, az * mag)

    /// <summary>Stops a body by clearing its forces and applying an opposing impulse proportional to its current momentum to cancel its velocity.</summary>
    [<McpServerTool>]
    [<Description("Stop a body by clearing forces and applying opposing impulse")>]
    static member stop_body(connection: GrpcConnection,
                            [<Description("Body ID to stop")>] body_id: string) =
        let clearResult = clearForces connection body_id
        match connection.LatestState with
        | None -> clearResult
        | Some state ->
            let body = state.Bodies |> Seq.tryFind (fun b -> b.Id = body_id)
            match body with
            | None -> clearResult
            | Some b ->
                let vx, vy, vz = b.Velocity.X, b.Velocity.Y, b.Velocity.Z
                let mass = b.Mass
                if mass > 0.0 && (abs vx > 0.001 || abs vy > 0.001 || abs vz > 0.001) then
                    applyImpulse connection body_id (-vx * mass, -vy * mass, -vz * mass)
                else
                    clearResult
