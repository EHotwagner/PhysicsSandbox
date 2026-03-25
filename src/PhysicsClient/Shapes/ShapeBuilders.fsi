module PhysicsClient.ShapeBuilders

open PhysicsSandbox.Shared.Contracts

/// Create a Sphere shape with the given radius.
val mkSphere : radius: float -> Shape

/// Create a Box shape with the given half-extents.
val mkBox : halfX: float -> halfY: float -> halfZ: float -> Shape

/// Create a Capsule shape with the given radius and length.
val mkCapsule : radius: float -> length: float -> Shape

/// Create a Cylinder shape with the given radius and length.
val mkCylinder : radius: float -> length: float -> Shape

/// Create a Plane shape with the given normal direction.
val mkPlane : normal: (float * float * float) -> Shape

/// Create a Triangle shape with three vertex positions.
val mkTriangle : a: (float * float * float) -> b: (float * float * float) -> c: (float * float * float) -> Shape
