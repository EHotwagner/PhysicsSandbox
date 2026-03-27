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

    /// <summary>Smoothly interpolate the camera from its current position to a target over a duration.</summary>
    [<McpServerTool; Description("Smoothly move the camera to a new position and target over a duration.")>]
    static member smooth_camera(
        conn: GrpcConnection,
        [<Description("Target camera position X.")>] pos_x: float,
        [<Description("Target camera position Y.")>] pos_y: float,
        [<Description("Target camera position Z.")>] pos_z: float,
        [<Description("Look-at target X.")>] target_x: float,
        [<Description("Look-at target Y.")>] target_y: float,
        [<Description("Look-at target Z.")>] target_z: float,
        [<Description("Animation duration in seconds.")>] duration_seconds: float,
        [<Description("Zoom level at end of animation. 0 = keep current zoom.")>] zoom_level: Nullable<float>
    ) : Task<string> =
        let cmd = SmoothCamera(
            Position = Vec3(X = pos_x, Y = pos_y, Z = pos_z),
            Target = Vec3(X = target_x, Y = target_y, Z = target_z),
            Up = Vec3(X = 0.0, Y = 1.0, Z = 0.0),
            DurationSeconds = duration_seconds,
            ZoomLevel = (if zoom_level.HasValue then zoom_level.Value else 0.0)
        )
        sendView conn (ViewCommand(SmoothCamera = cmd))

    /// <summary>Smoothly orient the camera to face a body.</summary>
    [<McpServerTool; Description("Smoothly orient the camera to look at a specific body.")>]
    static member camera_look_at(
        conn: GrpcConnection,
        [<Description("Body ID to look at.")>] body_id: string,
        [<Description("Animation duration in seconds. 0 = instant.")>] duration_seconds: float
    ) : Task<string> =
        sendView conn (ViewCommand(CameraLookAt = CameraLookAt(BodyId = body_id, DurationSeconds = duration_seconds)))

    /// <summary>Continuously track a body so the camera target follows it each frame.</summary>
    [<McpServerTool; Description("Continuously follow a body with the camera target.")>]
    static member camera_follow(
        conn: GrpcConnection,
        [<Description("Body ID to follow.")>] body_id: string
    ) : Task<string> =
        sendView conn (ViewCommand(CameraFollow = CameraFollow(BodyId = body_id)))

    /// <summary>Revolve the camera around a body over the specified duration.</summary>
    [<McpServerTool; Description("Orbit the camera around a body.")>]
    static member camera_orbit(
        conn: GrpcConnection,
        [<Description("Body ID to orbit around.")>] body_id: string,
        [<Description("Duration of the orbit animation in seconds.")>] duration_seconds: float,
        [<Description("Degrees to revolve. 0 = full 360.")>] degrees: Nullable<float>
    ) : Task<string> =
        let cmd = CameraOrbit(
            BodyId = body_id,
            DurationSeconds = duration_seconds,
            Degrees = (if degrees.HasValue then degrees.Value else 360.0)
        )
        sendView conn (ViewCommand(CameraOrbit = cmd))

    /// <summary>Continuously follow a body with a fixed relative offset (chase cam).</summary>
    [<McpServerTool; Description("Chase a body with a fixed camera offset.")>]
    static member camera_chase(
        conn: GrpcConnection,
        [<Description("Body ID to chase.")>] body_id: string,
        [<Description("Camera offset X relative to body.")>] offset_x: float,
        [<Description("Camera offset Y relative to body.")>] offset_y: float,
        [<Description("Camera offset Z relative to body.")>] offset_z: float
    ) : Task<string> =
        sendView conn (ViewCommand(CameraChase = CameraChase(BodyId = body_id, Offset = Vec3(X = offset_x, Y = offset_y, Z = offset_z))))

    /// <summary>Auto-position the camera to keep all listed bodies in view.</summary>
    [<McpServerTool; Description("Frame the camera to keep the specified bodies in view.")>]
    static member camera_frame_bodies(
        conn: GrpcConnection,
        [<Description("Comma-separated body IDs to frame.")>] body_ids: string
    ) : Task<string> =
        let cmd = CameraFrameBodies()
        for id in body_ids.Split(',') do
            cmd.BodyIds.Add(id.Trim())
        sendView conn (ViewCommand(CameraFrameBodies = cmd))

    /// <summary>Apply a camera shake effect.</summary>
    [<McpServerTool; Description("Shake the camera for dramatic effect.")>]
    static member camera_shake(
        conn: GrpcConnection,
        [<Description("Shake intensity.")>] intensity: float,
        [<Description("Shake duration in seconds.")>] duration_seconds: float
    ) : Task<string> =
        sendView conn (ViewCommand(CameraShake = CameraShake(Intensity = intensity, DurationSeconds = duration_seconds)))

    /// <summary>Cancel any active camera animation and hold the current position.</summary>
    [<McpServerTool; Description("Stop any active camera animation and hold the current position.")>]
    static member camera_stop(conn: GrpcConnection) : Task<string> =
        sendView conn (ViewCommand(CameraStop = CameraStop()))

    /// <summary>Set or clear the narration text overlay on the 3D viewer.</summary>
    [<McpServerTool; Description("Set or clear the narration text overlay. Pass empty string to clear.")>]
    static member set_narration(
        conn: GrpcConnection,
        [<Description("Narration text to display. Empty string clears it.")>] text: string
    ) : Task<string> =
        sendView conn (ViewCommand(SetNarration = SetNarration(Text = text)))

    /// <summary>Set demo metadata (name and description) displayed in the viewer overlay.</summary>
    [<McpServerTool; Description("Set demo name and description shown in the viewer overlay.")>]
    static member set_demo_metadata(
        conn: GrpcConnection,
        [<Description("Demo name.")>] name: string,
        [<Description("Demo description.")>] description: string
    ) : Task<string> =
        sendView conn (ViewCommand(SetDemoMetadata = SetDemoMetadata(Name = name, Description = description)))
