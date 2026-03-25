/// <summary>Conversion functions between F# tuples and protobuf <c>Vec3</c> messages.</summary>
/// <remarks>
/// The simulation uses a Y-up coordinate system: +Y is up, +X is right, +Z is toward the camera.
/// The ground plane sits at Y=0. Gravity defaults to (0, -9.81, 0).
/// </remarks>
module PhysicsSandbox.Scripting.Vec3Builders

open PhysicsSandbox.Shared.Contracts

/// <summary>Creates a protobuf <c>Vec3</c> from a float triple.</summary>
/// <param name="x">X component (horizontal axis). Positive = right.</param>
/// <param name="y">Y component (vertical axis). Positive = up. Ground is at Y=0; typical spawn heights are 1–20.</param>
/// <param name="z">Z component (depth axis). Positive = toward camera.</param>
/// <returns>A new <see cref="T:PhysicsSandbox.Shared.Contracts.Vec3"/> with the given coordinates.</returns>
/// <example>
/// <code>
/// let origin    = toVec3 (0.0, 0.0, 0.0)      // world origin (on the ground)
/// let aboveGround = toVec3 (0.0, 10.0, 0.0)   // 10 meters above ground
/// let gravity   = toVec3 (0.0, -9.81, 0.0)     // standard Earth gravity
/// let impulse   = toVec3 (0.0, 50.0, 0.0)      // strong upward impulse
/// </code>
/// </example>
let toVec3 (x: float, y: float, z: float) =
    PhysicsClient.Vec3Helpers.toVec3 (x, y, z)

/// <summary>Extracts the components of a <c>Vec3</c> as a float triple.</summary>
/// <param name="v">The Vec3 to decompose.</param>
/// <returns>A tuple of <c>(X, Y, Z)</c>.</returns>
/// <example>
/// <code>
/// let pos = toVec3 (1.0, 2.0, 3.0)
/// let (x, y, z) = toTuple pos   // x=1.0, y=2.0, z=3.0
/// </code>
/// </example>
let toTuple (v: Vec3) = PhysicsClient.Vec3Helpers.toTuple v
