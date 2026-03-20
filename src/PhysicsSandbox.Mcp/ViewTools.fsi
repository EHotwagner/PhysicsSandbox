module PhysicsSandbox.Mcp.ViewTools

open System.Threading.Tasks
open PhysicsSandbox.Mcp.GrpcConnection

[<Class>]
type ViewTools =
    static member set_camera : conn: GrpcConnection * ?pos_x: float * ?pos_y: float * ?pos_z: float * ?target_x: float * ?target_y: float * ?target_z: float -> Task<string>
    static member set_zoom : conn: GrpcConnection * level: float -> Task<string>
    static member toggle_wireframe : conn: GrpcConnection -> Task<string>
