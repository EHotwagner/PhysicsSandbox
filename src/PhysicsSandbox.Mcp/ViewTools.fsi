module PhysicsSandbox.Mcp.ViewTools

open System
open System.Threading.Tasks
open PhysicsSandbox.Mcp.GrpcConnection

[<Class>]
type ViewTools =
    static member set_camera : conn: GrpcConnection * pos_x: Nullable<float> * pos_y: Nullable<float> * pos_z: Nullable<float> * target_x: Nullable<float> * target_y: Nullable<float> * target_z: Nullable<float> -> Task<string>
    static member set_zoom : conn: GrpcConnection * level: float -> Task<string>
    static member toggle_wireframe : conn: GrpcConnection -> Task<string>
    static member smooth_camera : conn: GrpcConnection * pos_x: float * pos_y: float * pos_z: float * target_x: float * target_y: float * target_z: float * duration_seconds: float * zoom_level: Nullable<float> -> Task<string>
    static member camera_look_at : conn: GrpcConnection * body_id: string * duration_seconds: float -> Task<string>
    static member camera_follow : conn: GrpcConnection * body_id: string -> Task<string>
    static member camera_orbit : conn: GrpcConnection * body_id: string * duration_seconds: float * degrees: Nullable<float> -> Task<string>
    static member camera_chase : conn: GrpcConnection * body_id: string * offset_x: float * offset_y: float * offset_z: float -> Task<string>
    static member camera_frame_bodies : conn: GrpcConnection * body_ids: string -> Task<string>
    static member camera_shake : conn: GrpcConnection * intensity: float * duration_seconds: float -> Task<string>
    static member camera_stop : conn: GrpcConnection -> Task<string>
    static member set_narration : conn: GrpcConnection * text: string -> Task<string>
    static member set_demo_metadata : conn: GrpcConnection * name: string * description: string -> Task<string>
