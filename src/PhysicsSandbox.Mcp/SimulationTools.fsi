module PhysicsSandbox.Mcp.SimulationTools

open System.Threading.Tasks
open PhysicsSandbox.Mcp.GrpcConnection

/// Generate a unique body ID from a shape name (e.g. "sphere" -> "sphere-1")
val nextId : shape: string -> string

[<Class>]
type SimulationTools =
    static member add_body : conn: GrpcConnection * shape: string * ?radius: float * ?half_extents_x: float * ?half_extents_y: float * ?half_extents_z: float * ?capsule_radius: float * ?capsule_length: float * ?cylinder_radius: float * ?cylinder_length: float * ?tri_ax: float * ?tri_ay: float * ?tri_az: float * ?tri_bx: float * ?tri_by: float * ?tri_bz: float * ?tri_cx: float * ?tri_cy: float * ?tri_cz: float * ?plane_nx: float * ?plane_ny: float * ?plane_nz: float * ?x: float * ?y: float * ?z: float * ?mass: float * ?friction: float * ?max_recovery_velocity: float * ?spring_frequency: float * ?spring_damping_ratio: float * ?color_r: float * ?color_g: float * ?color_b: float * ?color_a: float * ?motion_type: string * ?collision_group: int * ?collision_mask: int -> Task<string>
    static member apply_force : conn: GrpcConnection * body_id: string * ?x: float * ?y: float * ?z: float -> Task<string>
    static member apply_impulse : conn: GrpcConnection * body_id: string * ?x: float * ?y: float * ?z: float -> Task<string>
    static member apply_torque : conn: GrpcConnection * body_id: string * ?x: float * ?y: float * ?z: float -> Task<string>
    static member set_gravity : conn: GrpcConnection * ?x: float * ?y: float * ?z: float -> Task<string>
    static member step : conn: GrpcConnection -> Task<string>
    static member play : conn: GrpcConnection -> Task<string>
    static member pause : conn: GrpcConnection -> Task<string>
    static member remove_body : conn: GrpcConnection * body_id: string -> Task<string>
    static member clear_forces : conn: GrpcConnection * body_id: string -> Task<string>
    static member restart_simulation : conn: GrpcConnection -> Task<string>
    static member add_constraint : conn: GrpcConnection * body_a: string * body_b: string * constraint_type: string * ?id: string * ?offset_ax: float * ?offset_ay: float * ?offset_az: float * ?offset_bx: float * ?offset_by: float * ?offset_bz: float * ?axis_x: float * ?axis_y: float * ?axis_z: float * ?axis_bx: float * ?axis_by: float * ?axis_bz: float * ?spring_frequency: float * ?spring_damping_ratio: float * ?min_distance: float * ?max_distance: float * ?target_distance: float * ?max_swing_angle: float * ?min_angle: float * ?max_angle: float * ?target_velocity: float * ?motor_max_force: float * ?motor_damping: float * ?angular_vel_x: float * ?angular_vel_y: float * ?angular_vel_z: float * ?weld_orient_x: float * ?weld_orient_y: float * ?weld_orient_z: float * ?weld_orient_w: float -> Task<string>
    static member remove_constraint : conn: GrpcConnection * constraint_id: string -> Task<string>
    static member register_shape : conn: GrpcConnection * shape_handle: string * shape: string * ?radius: float * ?half_extents_x: float * ?half_extents_y: float * ?half_extents_z: float * ?capsule_radius: float * ?capsule_length: float * ?cylinder_radius: float * ?cylinder_length: float * ?tri_ax: float * ?tri_ay: float * ?tri_az: float * ?tri_bx: float * ?tri_by: float * ?tri_bz: float * ?tri_cx: float * ?tri_cy: float * ?tri_cz: float -> Task<string>
    static member unregister_shape : conn: GrpcConnection * shape_handle: string -> Task<string>
    static member set_collision_filter : conn: GrpcConnection * body_id: string * collision_group: int * collision_mask: int -> Task<string>
    static member set_body_pose : conn: GrpcConnection * body_id: string * x: float * y: float * z: float * ?vx: float * ?vy: float * ?vz: float -> Task<string>
    static member raycast : conn: GrpcConnection * origin_x: float * origin_y: float * origin_z: float * direction_x: float * direction_y: float * direction_z: float * ?max_distance: float * ?collision_mask: int * ?all_hits: bool -> Task<string>
    static member sweep_cast : conn: GrpcConnection * shape: string * start_x: float * start_y: float * start_z: float * direction_x: float * direction_y: float * direction_z: float * ?max_distance: float * ?radius: float * ?half_extents_x: float * ?half_extents_y: float * ?half_extents_z: float * ?capsule_radius: float * ?capsule_length: float * ?collision_mask: int -> Task<string>
    static member overlap : conn: GrpcConnection * shape: string * x: float * y: float * z: float * ?radius: float * ?half_extents_x: float * ?half_extents_y: float * ?half_extents_z: float * ?capsule_radius: float * ?capsule_length: float * ?collision_mask: int -> Task<string>
