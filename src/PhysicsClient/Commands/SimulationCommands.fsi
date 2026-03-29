module PhysicsClient.SimulationCommands

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

val addSphere : session: Session -> position: (float * float * float) -> radius: float -> mass: float -> id: string option -> material: MaterialProperties option -> color: Color option -> motionType: BodyMotionType option -> collisionGroup: uint32 option -> collisionMask: uint32 option -> Result<string, string>
val addBox : session: Session -> position: (float * float * float) -> halfExtents: (float * float * float) -> mass: float -> id: string option -> material: MaterialProperties option -> color: Color option -> motionType: BodyMotionType option -> collisionGroup: uint32 option -> collisionMask: uint32 option -> Result<string, string>
val addCapsule : session: Session -> position: (float * float * float) -> radius: float -> length: float -> mass: float -> id: string option -> Result<string, string>
val addCylinder : session: Session -> position: (float * float * float) -> radius: float -> length: float -> mass: float -> id: string option -> Result<string, string>
val addPlane : session: Session -> normal: (float * float * float) option -> id: string option -> Result<string, string>
val addConstraint : session: Session -> id: string -> bodyA: string -> bodyB: string -> constraintType: ConstraintType -> Result<string, string>
val removeConstraint : session: Session -> constraintId: string -> Result<unit, string>
val registerShape : session: Session -> handle: string -> shape: Shape -> Result<unit, string>
val unregisterShape : session: Session -> handle: string -> Result<unit, string>
val setCollisionFilter : session: Session -> bodyId: string -> collisionGroup: uint32 -> collisionMask: uint32 -> Result<unit, string>
val removeBody : session: Session -> bodyId: string -> Result<unit, string>
val clearAll : session: Session -> Result<int, string>
val applyForce : session: Session -> bodyId: string -> force: (float * float * float) -> Result<unit, string>
val applyImpulse : session: Session -> bodyId: string -> impulse: (float * float * float) -> Result<unit, string>
val applyTorque : session: Session -> bodyId: string -> torque: (float * float * float) -> Result<unit, string>
val clearForces : session: Session -> bodyId: string -> Result<unit, string>
val setGravity : session: Session -> gravity: (float * float * float) -> Result<unit, string>
val play : session: Session -> Result<unit, string>
val pause : session: Session -> Result<unit, string>
val step : session: Session -> Result<unit, string>
val reset : session: Session -> Result<unit, string>
val confirmedReset : session: Session -> Result<ConfirmedResetResponse, string>
val setBodyPose : session: Session -> bodyId: string -> position: (float * float * float) -> orientation: (float * float * float * float) option -> velocity: (float * float * float) option -> angularVelocity: (float * float * float) option -> Result<unit, string>
val raycast : session: Session -> origin: (float * float * float) -> direction: (float * float * float) -> maxDistance: float -> allHits: bool -> collisionMask: uint32 option -> Result<RaycastResponse, string>
val sweepCast : session: Session -> shape: Shape -> startPosition: (float * float * float) -> direction: (float * float * float) -> maxDistance: float -> orientation: (float * float * float * float) option -> collisionMask: uint32 option -> Result<SweepCastResponse, string>
val overlap : session: Session -> shape: Shape -> position: (float * float * float) -> orientation: (float * float * float * float) option -> collisionMask: uint32 option -> Result<OverlapResponse, string>
val batchCommands : session: Session -> commands: SimulationCommand list -> Result<BatchResponse, string>
val batchViewCommands : session: Session -> commands: ViewCommand list -> Result<BatchResponse, string>
