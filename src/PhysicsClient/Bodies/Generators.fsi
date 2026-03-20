module PhysicsClient.Generators

open PhysicsClient.Session

val randomSpheres : session: Session -> count: int -> seed: int option -> Result<string list, string>
val randomBoxes : session: Session -> count: int -> seed: int option -> Result<string list, string>
val randomBodies : session: Session -> count: int -> seed: int option -> Result<string list, string>
val stack : session: Session -> count: int -> position: (float * float * float) option -> Result<string list, string>
val row : session: Session -> count: int -> position: (float * float * float) option -> Result<string list, string>
val grid : session: Session -> rows: int -> cols: int -> position: (float * float * float) option -> Result<string list, string>
val pyramid : session: Session -> layers: int -> position: (float * float * float) option -> Result<string list, string>
