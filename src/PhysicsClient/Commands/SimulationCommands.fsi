module PhysicsClient.SimulationCommands

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

val internal toVec3 : (float * float * float) -> Vec3

val addSphere : session: Session -> position: (float * float * float) -> radius: float -> mass: float -> id: string option -> Result<string, string>
val addBox : session: Session -> position: (float * float * float) -> halfExtents: (float * float * float) -> mass: float -> id: string option -> Result<string, string>
val addPlane : session: Session -> normal: (float * float * float) option -> id: string option -> Result<string, string>
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
val batchCommands : session: Session -> commands: SimulationCommand list -> Result<BatchResponse, string>
val batchViewCommands : session: Session -> commands: ViewCommand list -> Result<BatchResponse, string>
