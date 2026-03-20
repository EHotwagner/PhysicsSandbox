module PhysicsClient.ViewCommands

open PhysicsClient.Session

val setCamera : session: Session -> position: (float * float * float) -> target: (float * float * float) -> Result<unit, string>
val setZoom : session: Session -> level: float -> Result<unit, string>
val wireframe : session: Session -> enabled: bool -> Result<unit, string>
