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

let toVector3 (v: Vec3) =
    if isNull v then Vector3.Zero
    else Vector3(float32 v.X, float32 v.Y, float32 v.Z)

let fromVector3 (v: Vector3) =
    let r = Vec3()
    r.X <- double v.X
    r.Y <- double v.Y
    r.Z <- double v.Z
    r

let toQuaternion (v: Vec4) =
    if isNull v then Quaternion.Identity
    else Quaternion(float32 v.X, float32 v.Y, float32 v.Z, float32 v.W)

let fromQuaternion (q: Quaternion) =
    let r = Vec4()
    r.X <- double q.X
    r.Y <- double q.Y
    r.Z <- double q.Z
    r.W <- double q.W
    r
