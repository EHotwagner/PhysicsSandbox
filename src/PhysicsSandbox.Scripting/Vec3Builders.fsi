/// <summary>Conversion functions between F# tuples and protobuf <c>Vec3</c> messages.</summary>
/// <remarks>
/// The simulation uses a Y-up coordinate system: +Y is up, +X is right, +Z is toward the camera.
/// Ground plane is at Y=0. Gravity defaults to (0, -9.81, 0).
/// </remarks>
module PhysicsSandbox.Scripting.Vec3Builders

open PhysicsSandbox.Shared.Contracts

/// <summary>Creates a protobuf <c>Vec3</c> from a float triple.</summary>
/// <param name="x">X component (horizontal). Positive = right.</param>
/// <param name="y">Y component (vertical). Positive = up. Ground at Y=0; typical spawn heights 1–20.</param>
/// <param name="z">Z component (depth). Positive = toward camera.</param>
/// <returns>A new Vec3 with the given coordinates.</returns>
/// <example>
/// <code>
/// let origin = toVec3 (0.0, 0.0, 0.0)       // world origin
/// let above  = toVec3 (0.0, 10.0, 0.0)       // 10m above ground
/// let gravity = toVec3 (0.0, -9.81, 0.0)     // standard Earth gravity
/// </code>
/// </example>
val toVec3 : (float * float * float) -> Vec3

/// <summary>Extracts the components of a <c>Vec3</c> as a float triple <c>(X, Y, Z)</c>.</summary>
/// <param name="v">The Vec3 to decompose.</param>
/// <returns>A tuple of <c>(X, Y, Z)</c>.</returns>
val toTuple : Vec3 -> (float * float * float)
