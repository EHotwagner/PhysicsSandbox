module PhysicsSandbox.Mcp.QueryTools

open PhysicsSandbox.Mcp.GrpcConnection

[<Class>]
type QueryTools =
    static member get_state : conn: GrpcConnection -> string
    static member get_status : conn: GrpcConnection -> string
