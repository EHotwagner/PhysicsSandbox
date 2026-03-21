module PhysicsSandbox.Mcp.PresetTools

open System.ComponentModel
open System.Threading
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection
open PhysicsSandbox.Mcp.ClientAdapter

let mutable private presetCount = 0

let private nextPresetId (prefix: string) =
    let n = Interlocked.Increment(&presetCount)
    $"{prefix}-{n}"

[<McpServerToolType>]
type PresetTools() =

    [<McpServerTool>]
    [<Description("Add a marble (tiny sphere, 0.01 radius, 0.005 mass)")>]
    static member add_marble(connection: GrpcConnection,
                             [<Description("X position (default 0)")>] x: float,
                             [<Description("Y position (default 0)")>] y: float,
                             [<Description("Z position (default 0)")>] z: float,
                             [<Description("Mass override (default 0.005)")>] mass: float,
                             [<Description("Custom body ID (auto-generated if empty)")>] id: string) =
        let pos = (x, y, z)
        let m = if mass <= 0.0 then 0.005 else mass
        let bodyId = if System.String.IsNullOrEmpty(id) then nextPresetId "marble" else id
        addSphere connection pos 0.01 m bodyId

    [<McpServerTool>]
    [<Description("Add a bowling ball (0.11 radius, 6.35 mass)")>]
    static member add_bowling_ball(connection: GrpcConnection,
                                   [<Description("X position (default 0)")>] x: float,
                                   [<Description("Y position (default 0)")>] y: float,
                                   [<Description("Z position (default 0)")>] z: float,
                                   [<Description("Mass override (default 6.35)")>] mass: float,
                                   [<Description("Custom body ID (auto-generated if empty)")>] id: string) =
        let pos = (x, y, z)
        let m = if mass <= 0.0 then 6.35 else mass
        let bodyId = if System.String.IsNullOrEmpty(id) then nextPresetId "bowlingball" else id
        addSphere connection pos 0.11 m bodyId

    [<McpServerTool>]
    [<Description("Add a beach ball (0.2 radius, 0.1 mass)")>]
    static member add_beach_ball(connection: GrpcConnection,
                                 [<Description("X position (default 0)")>] x: float,
                                 [<Description("Y position (default 0)")>] y: float,
                                 [<Description("Z position (default 0)")>] z: float,
                                 [<Description("Mass override (default 0.1)")>] mass: float,
                                 [<Description("Custom body ID (auto-generated if empty)")>] id: string) =
        let pos = (x, y, z)
        let m = if mass <= 0.0 then 0.1 else mass
        let bodyId = if System.String.IsNullOrEmpty(id) then nextPresetId "beachball" else id
        addSphere connection pos 0.2 m bodyId

    [<McpServerTool>]
    [<Description("Add a crate (0.5x0.5x0.5 box, 20 mass)")>]
    static member add_crate(connection: GrpcConnection,
                            [<Description("X position (default 0)")>] x: float,
                            [<Description("Y position (default 0)")>] y: float,
                            [<Description("Z position (default 0)")>] z: float,
                            [<Description("Mass override (default 20)")>] mass: float,
                            [<Description("Custom body ID (auto-generated if empty)")>] id: string) =
        let pos = (x, y, z)
        let m = if mass <= 0.0 then 20.0 else mass
        let bodyId = if System.String.IsNullOrEmpty(id) then nextPresetId "crate" else id
        addBox connection pos (0.5, 0.5, 0.5) m bodyId

    [<McpServerTool>]
    [<Description("Add a brick (0.2x0.1x0.05 box, 3 mass)")>]
    static member add_brick(connection: GrpcConnection,
                            [<Description("X position (default 0)")>] x: float,
                            [<Description("Y position (default 0)")>] y: float,
                            [<Description("Z position (default 0)")>] z: float,
                            [<Description("Mass override (default 3)")>] mass: float,
                            [<Description("Custom body ID (auto-generated if empty)")>] id: string) =
        let pos = (x, y, z)
        let m = if mass <= 0.0 then 3.0 else mass
        let bodyId = if System.String.IsNullOrEmpty(id) then nextPresetId "brick" else id
        addBox connection pos (0.2, 0.1, 0.05) m bodyId

    [<McpServerTool>]
    [<Description("Add a boulder (0.5 radius sphere, 200 mass)")>]
    static member add_boulder(connection: GrpcConnection,
                              [<Description("X position (default 0)")>] x: float,
                              [<Description("Y position (default 0)")>] y: float,
                              [<Description("Z position (default 0)")>] z: float,
                              [<Description("Mass override (default 200)")>] mass: float,
                              [<Description("Custom body ID (auto-generated if empty)")>] id: string) =
        let pos = (x, y, z)
        let m = if mass <= 0.0 then 200.0 else mass
        let bodyId = if System.String.IsNullOrEmpty(id) then nextPresetId "boulder" else id
        addSphere connection pos 0.5 m bodyId

    [<McpServerTool>]
    [<Description("Add a die (0.05x0.05x0.05 box, 0.03 mass)")>]
    static member add_die(connection: GrpcConnection,
                          [<Description("X position (default 0)")>] x: float,
                          [<Description("Y position (default 0)")>] y: float,
                          [<Description("Z position (default 0)")>] z: float,
                          [<Description("Mass override (default 0.03)")>] mass: float,
                          [<Description("Custom body ID (auto-generated if empty)")>] id: string) =
        let pos = (x, y, z)
        let m = if mass <= 0.0 then 0.03 else mass
        let bodyId = if System.String.IsNullOrEmpty(id) then nextPresetId "die" else id
        addBox connection pos (0.05, 0.05, 0.05) m bodyId
