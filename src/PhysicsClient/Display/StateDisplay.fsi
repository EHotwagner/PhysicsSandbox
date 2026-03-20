module PhysicsClient.StateDisplay

open PhysicsClient.Session

val listBodies : session: Session -> unit
val inspect : session: Session -> bodyId: string -> unit
val status : session: Session -> unit
val snapshot : session: Session -> PhysicsSandbox.Shared.Contracts.SimulationState option

/// Internal helpers for testing
val internal formatVec3 : PhysicsSandbox.Shared.Contracts.Vec3 -> string
val internal velocityMagnitude : PhysicsSandbox.Shared.Contracts.Vec3 -> float
val internal shapeDescription : PhysicsSandbox.Shared.Contracts.Body -> string
