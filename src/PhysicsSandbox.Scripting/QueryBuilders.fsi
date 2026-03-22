/// <summary>Convenience wrappers for physics query operations (raycast, sweep cast, overlap).</summary>
module PhysicsSandbox.Scripting.QueryBuilders

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

/// <summary>Casts a ray and returns hit results.</summary>
/// <param name="session">Active session.</param>
/// <param name="origin">Ray origin <c>(x, y, z)</c>.</param>
/// <param name="direction">Ray direction <c>(x, y, z)</c>.</param>
/// <param name="maxDistance">Maximum ray distance. Default: 1000.</param>
/// <returns>List of (bodyId, position, normal, distance) tuples for each hit.</returns>
val raycast : session: Session -> origin: (float * float * float) -> direction: (float * float * float) -> maxDistance: float -> (string * (float * float * float) * (float * float * float) * float) list

/// <summary>Casts a ray and returns all hits along the ray.</summary>
val raycastAll : session: Session -> origin: (float * float * float) -> direction: (float * float * float) -> maxDistance: float -> (string * (float * float * float) * (float * float * float) * float) list

/// <summary>Performs a sphere sweep cast.</summary>
/// <param name="session">Active session.</param>
/// <param name="radius">Sphere radius.</param>
/// <param name="startPosition">Start position <c>(x, y, z)</c>.</param>
/// <param name="direction">Sweep direction <c>(x, y, z)</c>.</param>
/// <param name="maxDistance">Maximum sweep distance.</param>
/// <returns>Some (bodyId, position, normal, distance) if hit, None otherwise.</returns>
val sweepSphere : session: Session -> radius: float -> startPosition: (float * float * float) -> direction: (float * float * float) -> maxDistance: float -> (string * (float * float * float) * (float * float * float) * float) option

/// <summary>Tests for overlapping bodies at a position using a sphere shape.</summary>
/// <param name="session">Active session.</param>
/// <param name="radius">Sphere radius.</param>
/// <param name="position">Test position <c>(x, y, z)</c>.</param>
/// <returns>List of overlapping body IDs.</returns>
val overlapSphere : session: Session -> radius: float -> position: (float * float * float) -> string list
