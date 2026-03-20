module PhysicsClient.Presets

open PhysicsClient.Session

val marble : session: Session -> position: (float * float * float) option -> mass: float option -> id: string option -> Result<string, string>
val bowlingBall : session: Session -> position: (float * float * float) option -> mass: float option -> id: string option -> Result<string, string>
val beachBall : session: Session -> position: (float * float * float) option -> mass: float option -> id: string option -> Result<string, string>
val crate : session: Session -> position: (float * float * float) option -> mass: float option -> id: string option -> Result<string, string>
val brick : session: Session -> position: (float * float * float) option -> mass: float option -> id: string option -> Result<string, string>
val boulder : session: Session -> position: (float * float * float) option -> mass: float option -> id: string option -> Result<string, string>
val die : session: Session -> position: (float * float * float) option -> mass: float option -> id: string option -> Result<string, string>
