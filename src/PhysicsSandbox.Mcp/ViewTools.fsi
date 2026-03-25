module PhysicsSandbox.Mcp.ViewTools

open System
open System.Threading.Tasks
open PhysicsSandbox.Mcp.GrpcConnection

[<Class>]
type ViewTools =
    static member set_camera : conn: GrpcConnection * pos_x: Nullable<float> * pos_y: Nullable<float> * pos_z: Nullable<float> * target_x: Nullable<float> * target_y: Nullable<float> * target_z: Nullable<float> -> Task<string>
    static member set_zoom : conn: GrpcConnection * level: float -> Task<string>
    static member toggle_wireframe : conn: GrpcConnection -> Task<string>
