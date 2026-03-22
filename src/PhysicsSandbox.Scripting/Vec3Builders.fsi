module PhysicsSandbox.Scripting.Vec3Builders

open PhysicsSandbox.Shared.Contracts

val toVec3 : (float * float * float) -> Vec3
val toTuple : Vec3 -> (float * float * float)
