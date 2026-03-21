namespace PhysicsSandbox.Mcp

open System.ComponentModel
open System.Threading.Tasks
open ModelContextProtocol.Server

[<McpServerToolType; Class>]
type BatchTools =
    [<McpServerTool>]
    static member batch_commands : conn: GrpcConnection.GrpcConnection * [<Description("JSON array of commands, e.g. [{\"type\":\"add_body\",\"shape\":\"sphere\",\"radius\":0.5,\"x\":0,\"y\":5,\"z\":0,\"mass\":1},{\"type\":\"step\"}]")>] commands: string -> Task<string>

    [<McpServerTool>]
    static member batch_view_commands : conn: GrpcConnection.GrpcConnection * [<Description("JSON array of view commands, e.g. [{\"type\":\"set_camera\",\"px\":0,\"py\":10,\"pz\":20,\"tx\":0,\"ty\":0,\"tz\":0}]")>] commands: string -> Task<string>
