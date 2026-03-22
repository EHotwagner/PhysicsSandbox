/// <summary>Conversion functions between F# tuples and protobuf <c>Vec3</c> messages.</summary>
module PhysicsSandbox.Scripting.Vec3Builders

open PhysicsSandbox.Shared.Contracts

/// <summary>Creates a protobuf <c>Vec3</c> from a float triple.</summary>
/// <param name="x">X component.</param>
/// <param name="y">Y component.</param>
/// <param name="z">Z component.</param>
/// <returns>A new <see cref="T:PhysicsSandbox.Shared.Contracts.Vec3"/> with the given coordinates.</returns>
let toVec3 (x: float, y: float, z: float) =
    let v = Vec3()
    v.X <- x; v.Y <- y; v.Z <- z
    v

/// <summary>Extracts the components of a <c>Vec3</c> as a float triple.</summary>
/// <param name="v">The Vec3 to decompose.</param>
/// <returns>A tuple of <c>(X, Y, Z)</c>.</returns>
let toTuple (v: Vec3) = (v.X, v.Y, v.Z)
