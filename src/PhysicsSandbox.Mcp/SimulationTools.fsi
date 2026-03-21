module PhysicsSandbox.Mcp.SimulationTools

open System.Threading.Tasks
open PhysicsSandbox.Mcp.GrpcConnection

/// Generate a unique body ID from a shape name (e.g. "sphere" → "sphere-1")
val nextId : shape: string -> string

[<Class>]
type SimulationTools =
    static member add_body : conn: GrpcConnection * shape: string * ?radius: float * ?half_extents_x: float * ?half_extents_y: float * ?half_extents_z: float * ?x: float * ?y: float * ?z: float * ?mass: float -> Task<string>
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
