module PhysicsSimulation.ProtoConversions

open System.Numerics
open PhysicsSandbox.Shared.Contracts

// Proto type aliases to avoid naming conflicts with BepuFSharp types
type ProtoSphere = PhysicsSandbox.Shared.Contracts.Sphere
type ProtoBox = PhysicsSandbox.Shared.Contracts.Box
type ProtoCapsule = PhysicsSandbox.Shared.Contracts.Capsule
type ProtoCylinder = PhysicsSandbox.Shared.Contracts.Cylinder
type ProtoTriangle = PhysicsSandbox.Shared.Contracts.Triangle
type ProtoColor = PhysicsSandbox.Shared.Contracts.Color
type ProtoMaterialProperties = PhysicsSandbox.Shared.Contracts.MaterialProperties
type ProtoCachedShapeRef = PhysicsSandbox.Shared.Contracts.CachedShapeRef

/// Convert proto Vec3 to System.Numerics.Vector3 (null-safe, returns Zero)
val toVector3 : Vec3 -> Vector3

/// Convert System.Numerics.Vector3 to proto Vec3
val fromVector3 : Vector3 -> Vec3

/// Convert proto Vec4 to System.Numerics.Quaternion (null-safe, returns Identity)
val toQuaternion : Vec4 -> Quaternion

/// Convert System.Numerics.Quaternion to proto Vec4
val fromQuaternion : Quaternion -> Vec4
