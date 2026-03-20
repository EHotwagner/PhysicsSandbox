module PhysicsSandbox.Mcp.ViewTools

open System.ComponentModel
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Mcp.GrpcConnection

let private sendView (conn: GrpcConnection) (cmd: ViewCommand) =
    task {
        try
            let! ack = conn.Client.SendViewCommandAsync(cmd)
            if ack.Success then return $"Success: {ack.Message}"
            else return $"Failed: {ack.Message}"
        with ex ->
            return $"Error: {ex.Message}"
    }

[<McpServerToolType>]
type ViewTools() =

    [<McpServerTool; Description("Set the 3D viewer camera position and target.")>]
    static member set_camera(
        conn: GrpcConnection,
        [<Description("Camera position X")>] ?pos_x: float,
        [<Description("Camera position Y")>] ?pos_y: float,
        [<Description("Camera position Z")>] ?pos_z: float,
        [<Description("Look-at target X")>] ?target_x: float,
        [<Description("Look-at target Y")>] ?target_y: float,
        [<Description("Look-at target Z")>] ?target_z: float
    ) : Task<string> =
        let cmd = SetCamera(
            Position = Vec3(X = defaultArg pos_x 0.0, Y = defaultArg pos_y 10.0, Z = defaultArg pos_z 20.0),
            Target = Vec3(X = defaultArg target_x 0.0, Y = defaultArg target_y 0.0, Z = defaultArg target_z 0.0)
        )
        sendView conn (ViewCommand(SetCamera = cmd))

    [<McpServerTool; Description("Set the 3D viewer zoom level.")>]
    static member set_zoom(
        conn: GrpcConnection,
        [<Description("Zoom level (1.0 = default)")>] level: float
    ) : Task<string> =
        sendView conn (ViewCommand(SetZoom = SetZoom(Level = level)))

    [<McpServerTool; Description("Toggle wireframe rendering mode in the 3D viewer.")>]
    static member toggle_wireframe(conn: GrpcConnection) : Task<string> =
        sendView conn (ViewCommand(ToggleWireframe = ToggleWireframe()))
