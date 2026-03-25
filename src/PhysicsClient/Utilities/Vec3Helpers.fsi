module PhysicsClient.Vec3Helpers

open PhysicsSandbox.Shared.Contracts

/// Convert an F# float triple to a protobuf Vec3 message.
val toVec3 : (float * float * float) -> Vec3

/// Extract the components of a Vec3 as a float triple (X, Y, Z).
val toTuple : Vec3 -> (float * float * float)
