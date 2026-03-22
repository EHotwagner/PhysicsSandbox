[<AutoOpen>]
module PhysicsSandbox.Scripting.Prelude

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

val ok : Result<'a, string> -> 'a
val sleep : int -> unit
val timed : string -> (unit -> 'a) -> 'a
val toVec3 : (float * float * float) -> Vec3
val toTuple : Vec3 -> (float * float * float)
val makeSphereCmd : id: string -> pos: (float * float * float) -> radius: float -> mass: float -> SimulationCommand
val makeBoxCmd : id: string -> pos: (float * float * float) -> halfExtents: (float * float * float) -> mass: float -> SimulationCommand
val makeImpulseCmd : bodyId: string -> impulse: (float * float * float) -> SimulationCommand
val makeTorqueCmd : bodyId: string -> torque: (float * float * float) -> SimulationCommand
val batchAdd : Session -> SimulationCommand list -> unit
val resetSimulation : Session -> unit
val runFor : Session -> float -> unit
val nextId : string -> string
