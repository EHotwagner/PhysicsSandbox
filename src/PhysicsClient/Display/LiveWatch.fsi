module PhysicsClient.LiveWatch

open PhysicsClient.Session

val watch : session: Session -> bodyIds: string list option -> shapeFilter: string option -> minVelocity: float option -> unit
