module PhysicsClient.ShapeBuilders

open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Vec3Helpers

let mkSphere (radius: float) =
    let s = Sphere()
    s.Radius <- radius
    Shape(Sphere = s)

let mkBox (halfX: float) (halfY: float) (halfZ: float) =
    let b = Box()
    b.HalfExtents <- toVec3 (halfX, halfY, halfZ)
    Shape(Box = b)

let mkCapsule (radius: float) (length: float) =
    let c = Capsule()
    c.Radius <- radius
    c.Length <- length
    Shape(Capsule = c)

let mkCylinder (radius: float) (length: float) =
    let c = Cylinder()
    c.Radius <- radius
    c.Length <- length
    Shape(Cylinder = c)

let mkPlane (normal: float * float * float) =
    let p = Plane()
    p.Normal <- toVec3 normal
    Shape(Plane = p)

let mkTriangle (a: float * float * float) (b: float * float * float) (c: float * float * float) =
    let t = Triangle()
    t.A <- toVec3 a
    t.B <- toVec3 b
    t.C <- toVec3 c
    Shape(Triangle = t)
