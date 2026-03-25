module PhysicsClient.Vec3Helpers

open PhysicsSandbox.Shared.Contracts

let toVec3 (x: float, y: float, z: float) =
    let v = Vec3()
    v.X <- x; v.Y <- y; v.Z <- z
    v

let toTuple (v: Vec3) = (v.X, v.Y, v.Z)
