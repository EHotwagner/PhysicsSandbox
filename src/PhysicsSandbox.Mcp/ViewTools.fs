/// <summary>MCP tool class for controlling the 3D viewer: camera positioning, zoom, and wireframe rendering.</summary>
module PhysicsSandbox.Mcp.ViewTools

open System
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

/// <summary>MCP server tool type providing 3D viewer controls for camera, zoom, and rendering mode.</summary>
[<McpServerToolType>]
type ViewTools() =

    /// <summary>Sets the 3D viewer camera position and look-at target. Unspecified components default to a sensible overhead view.</summary>
    [<McpServerTool; Description("Set the 3D viewer camera position and target.")>]
    static member set_camera(
        conn: GrpcConnection,
        [<Description("Camera position X. Default: 0.")>] pos_x: Nullable<float>,
        [<Description("Camera position Y. Default: 10.")>] pos_y: Nullable<float>,
        [<Description("Camera position Z. Default: 20.")>] pos_z: Nullable<float>,
        [<Description("Look-at target X. Default: 0.")>] target_x: Nullable<float>,
        [<Description("Look-at target Y. Default: 0.")>] target_y: Nullable<float>,
        [<Description("Look-at target Z. Default: 0.")>] target_z: Nullable<float>
    ) : Task<string> =
        let cmd = SetCamera(
            Position = Vec3(X = (if pos_x.HasValue then pos_x.Value else 0.0), Y = (if pos_y.HasValue then pos_y.Value else 10.0), Z = (if pos_z.HasValue then pos_z.Value else 20.0)),
            Target = Vec3(X = (if target_x.HasValue then target_x.Value else 0.0), Y = (if target_y.HasValue then target_y.Value else 0.0), Z = (if target_z.HasValue then target_z.Value else 0.0))
        )
        sendView conn (ViewCommand(SetCamera = cmd))

    /// <summary>Sets the 3D viewer zoom level, where 1.0 is the default zoom.</summary>
    [<McpServerTool; Description("Set the 3D viewer zoom level.")>]
    static member set_zoom(
        conn: GrpcConnection,
        [<Description("Zoom level. 1.0 = default zoom. Values > 1 zoom in, < 1 zoom out.")>] level: float
    ) : Task<string> =
        sendView conn (ViewCommand(SetZoom = SetZoom(Level = level)))

    /// <summary>Toggles wireframe rendering mode on or off in the 3D viewer.</summary>
    [<McpServerTool; Description("Toggle wireframe rendering mode in the 3D viewer.")>]
    static member toggle_wireframe(conn: GrpcConnection) : Task<string> =
        sendView conn (ViewCommand(ToggleWireframe = ToggleWireframe()))
