module PhysicsSimulation.MeshIdGenerator

open PhysicsSandbox.Shared.Contracts

/// Compute a content-addressed mesh ID (32 hex chars, SHA-256 truncated to 128 bits)
/// for complex shapes (ConvexHull, MeshShape, Compound). Returns None for primitives.
val computeMeshId : Shape -> string option

/// Compute the axis-aligned bounding box for complex shapes.
/// Returns (bbox_min, bbox_max) as Vec3 pairs, or None for primitives.
val computeBoundingBox : Shape -> (Vec3 * Vec3) option
