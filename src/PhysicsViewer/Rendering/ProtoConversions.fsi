module PhysicsViewer.Rendering.ProtoConversions

open Stride.Core.Mathematics
open PhysicsSandbox.Shared.Contracts

/// Convert proto Vec3 to Stride Vector3 (null-safe, returns Zero)
val protoVec3ToStride : Vec3 -> Vector3

/// Convert proto Vec4 to Stride Quaternion (null-safe, returns Identity)
val protoQuatToStride : Vec4 -> Quaternion
