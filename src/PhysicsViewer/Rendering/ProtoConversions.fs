module PhysicsViewer.Rendering.ProtoConversions

open Stride.Core.Mathematics
open PhysicsSandbox.Shared.Contracts

let protoVec3ToStride (v: Vec3) =
    if isNull v then Vector3.Zero
    else Vector3(float32 v.X, float32 v.Y, float32 v.Z)

let protoQuatToStride (v: Vec4) =
    if isNull v then Quaternion.Identity
    else Quaternion(float32 v.X, float32 v.Y, float32 v.Z, float32 v.W)
