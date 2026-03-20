module PhysicsClient.Steering

open PhysicsClient.Session

type Direction = Up | Down | North | South | East | West

val internal directionToVec : Direction -> (float * float * float)

val push : session: Session -> bodyId: string -> direction: Direction -> magnitude: float -> Result<unit, string>
val pushVec : session: Session -> bodyId: string -> vector: (float * float * float) -> Result<unit, string>
val launch : session: Session -> bodyId: string -> target: (float * float * float) -> speed: float -> Result<unit, string>
val spin : session: Session -> bodyId: string -> axis: Direction -> magnitude: float -> Result<unit, string>
val stop : session: Session -> bodyId: string -> Result<unit, string>
