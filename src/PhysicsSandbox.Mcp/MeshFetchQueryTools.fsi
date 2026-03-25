module PhysicsSandbox.Mcp.MeshFetchQueryTools

open System
open ModelContextProtocol.Server

[<McpServerToolType>]
[<Class>]
type MeshFetchQueryTools =
    [<McpServerTool>]
    static member query_mesh_fetches:
        engine: PhysicsSandbox.Mcp.Recording.RecordingEngine.RecordingEngine *
        session_id: string *
        minutes_ago: Nullable<int> *
        mesh_id: string *
        page_size: Nullable<int> *
        cursor: string -> string
