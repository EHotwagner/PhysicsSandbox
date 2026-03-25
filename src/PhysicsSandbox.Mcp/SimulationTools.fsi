module PhysicsSandbox.Mcp.SimulationTools

open System
open System.Threading.Tasks
open PhysicsSandbox.Mcp.GrpcConnection

[<Class>]
type SimulationTools =
    static member add_body : conn: GrpcConnection * shape: string * radius: Nullable<float> * half_extents_x: Nullable<float> * half_extents_y: Nullable<float> * half_extents_z: Nullable<float> * capsule_radius: Nullable<float> * capsule_length: Nullable<float> * cylinder_radius: Nullable<float> * cylinder_length: Nullable<float> * tri_ax: Nullable<float> * tri_ay: Nullable<float> * tri_az: Nullable<float> * tri_bx: Nullable<float> * tri_by: Nullable<float> * tri_bz: Nullable<float> * tri_cx: Nullable<float> * tri_cy: Nullable<float> * tri_cz: Nullable<float> * plane_nx: Nullable<float> * plane_ny: Nullable<float> * plane_nz: Nullable<float> * x: Nullable<float> * y: Nullable<float> * z: Nullable<float> * mass: Nullable<float> * friction: Nullable<float> * max_recovery_velocity: Nullable<float> * spring_frequency: Nullable<float> * spring_damping_ratio: Nullable<float> * color_r: Nullable<float> * color_g: Nullable<float> * color_b: Nullable<float> * color_a: Nullable<float> * motion_type: string * collision_group: Nullable<int> * collision_mask: Nullable<int> -> Task<string>
    static member apply_force : conn: GrpcConnection * body_id: string * x: Nullable<float> * y: Nullable<float> * z: Nullable<float> -> Task<string>
    static member apply_impulse : conn: GrpcConnection * body_id: string * x: Nullable<float> * y: Nullable<float> * z: Nullable<float> -> Task<string>
    static member apply_torque : conn: GrpcConnection * body_id: string * x: Nullable<float> * y: Nullable<float> * z: Nullable<float> -> Task<string>
    static member set_gravity : conn: GrpcConnection * x: Nullable<float> * y: Nullable<float> * z: Nullable<float> -> Task<string>
    static member step : conn: GrpcConnection -> Task<string>
    static member play : conn: GrpcConnection -> Task<string>
    static member pause : conn: GrpcConnection -> Task<string>
    static member remove_body : conn: GrpcConnection * body_id: string -> Task<string>
    static member clear_forces : conn: GrpcConnection * body_id: string -> Task<string>
    static member restart_simulation : conn: GrpcConnection -> Task<string>
    static member add_constraint : conn: GrpcConnection * body_a: string * body_b: string * constraint_type: string * id: string * offset_ax: Nullable<float> * offset_ay: Nullable<float> * offset_az: Nullable<float> * offset_bx: Nullable<float> * offset_by: Nullable<float> * offset_bz: Nullable<float> * axis_x: Nullable<float> * axis_y: Nullable<float> * axis_z: Nullable<float> * axis_bx: Nullable<float> * axis_by: Nullable<float> * axis_bz: Nullable<float> * spring_frequency: Nullable<float> * spring_damping_ratio: Nullable<float> * min_distance: Nullable<float> * max_distance: Nullable<float> * target_distance: Nullable<float> * max_swing_angle: Nullable<float> * min_angle: Nullable<float> * max_angle: Nullable<float> * target_velocity: Nullable<float> * motor_max_force: Nullable<float> * motor_damping: Nullable<float> * angular_vel_x: Nullable<float> * angular_vel_y: Nullable<float> * angular_vel_z: Nullable<float> * weld_orient_x: Nullable<float> * weld_orient_y: Nullable<float> * weld_orient_z: Nullable<float> * weld_orient_w: Nullable<float> -> Task<string>
    static member remove_constraint : conn: GrpcConnection * constraint_id: string -> Task<string>
    static member register_shape : conn: GrpcConnection * shape_handle: string * shape: string * radius: Nullable<float> * half_extents_x: Nullable<float> * half_extents_y: Nullable<float> * half_extents_z: Nullable<float> * capsule_radius: Nullable<float> * capsule_length: Nullable<float> * cylinder_radius: Nullable<float> * cylinder_length: Nullable<float> * tri_ax: Nullable<float> * tri_ay: Nullable<float> * tri_az: Nullable<float> * tri_bx: Nullable<float> * tri_by: Nullable<float> * tri_bz: Nullable<float> * tri_cx: Nullable<float> * tri_cy: Nullable<float> * tri_cz: Nullable<float> -> Task<string>
    static member unregister_shape : conn: GrpcConnection * shape_handle: string -> Task<string>
    static member set_collision_filter : conn: GrpcConnection * body_id: string * collision_group: int * collision_mask: int -> Task<string>
    static member set_body_pose : conn: GrpcConnection * body_id: string * x: float * y: float * z: float * vx: Nullable<float> * vy: Nullable<float> * vz: Nullable<float> -> Task<string>
    static member raycast : conn: GrpcConnection * origin_x: float * origin_y: float * origin_z: float * direction_x: float * direction_y: float * direction_z: float * max_distance: Nullable<float> * collision_mask: Nullable<int> * all_hits: Nullable<bool> -> Task<string>
    static member sweep_cast : conn: GrpcConnection * shape: string * start_x: float * start_y: float * start_z: float * direction_x: float * direction_y: float * direction_z: float * max_distance: Nullable<float> * radius: Nullable<float> * half_extents_x: Nullable<float> * half_extents_y: Nullable<float> * half_extents_z: Nullable<float> * capsule_radius: Nullable<float> * capsule_length: Nullable<float> * collision_mask: Nullable<int> -> Task<string>
    static member overlap : conn: GrpcConnection * shape: string * x: float * y: float * z: float * radius: Nullable<float> * half_extents_x: Nullable<float> * half_extents_y: Nullable<float> * half_extents_z: Nullable<float> * capsule_radius: Nullable<float> * capsule_length: Nullable<float> * collision_mask: Nullable<int> -> Task<string>
