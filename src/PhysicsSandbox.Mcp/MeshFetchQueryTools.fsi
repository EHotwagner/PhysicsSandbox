module PhysicsSandbox.Mcp.MeshFetchQueryTools

open ModelContextProtocol.Server

[<McpServerToolType>]
[<Class>]
type MeshFetchQueryTools =
    [<McpServerTool>]
    static member query_mesh_fetches:
        engine: PhysicsSandbox.Mcp.Recording.RecordingEngine.RecordingEngine *
        session_id: string *
        minutes_ago: int *
        mesh_id: string *
        page_size: int *
        cursor: string -> string
