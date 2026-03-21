module PhysicsSandbox.Mcp.ClientAdapter

open PhysicsSandbox.Mcp.GrpcConnection
open PhysicsSandbox.Shared.Contracts

/// Send a simulation command via the shared GrpcConnection.
val sendCommand : GrpcConnection -> SimulationCommand -> string

/// Create a Vec3 from tuple.
val toVec3 : (float * float * float) -> Vec3

/// Add a sphere body. Returns the body ID.
val addSphere : GrpcConnection -> position: (float * float * float) -> radius: float -> mass: float -> id: string -> string

/// Add a box body. Returns the body ID.
val addBox : GrpcConnection -> position: (float * float * float) -> halfExtents: (float * float * float) -> mass: float -> id: string -> string

/// Apply an impulse to a body.
val applyImpulse : GrpcConnection -> bodyId: string -> impulse: (float * float * float) -> string

/// Apply a torque to a body.
val applyTorque : GrpcConnection -> bodyId: string -> torque: (float * float * float) -> string

/// Clear forces on a body.
val clearForces : GrpcConnection -> bodyId: string -> string
