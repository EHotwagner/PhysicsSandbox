module PhysicsClient.ViewCommands

open PhysicsClient.Session

val setCamera : session: Session -> position: (float * float * float) -> target: (float * float * float) -> Result<unit, string>
val setZoom : session: Session -> level: float -> Result<unit, string>
val wireframe : session: Session -> enabled: bool -> Result<unit, string>
val setDemoMetadata : session: Session -> name: string -> description: string -> Result<unit, string>
val smoothCamera : session: Session -> position: (float * float * float) -> target: (float * float * float) -> durationSeconds: float -> Result<unit, string>
val smoothCameraWithZoom : session: Session -> position: (float * float * float) -> target: (float * float * float) -> durationSeconds: float -> zoomLevel: float -> Result<unit, string>
val cameraLookAt : session: Session -> bodyId: string -> durationSeconds: float -> Result<unit, string>
val cameraFollow : session: Session -> bodyId: string -> Result<unit, string>
val cameraOrbit : session: Session -> bodyId: string -> durationSeconds: float -> degrees: float -> Result<unit, string>
val cameraChase : session: Session -> bodyId: string -> offset: (float * float * float) -> Result<unit, string>
val cameraFrameBodies : session: Session -> bodyIds: string list -> Result<unit, string>
val cameraShake : session: Session -> intensity: float -> durationSeconds: float -> Result<unit, string>
val cameraStop : session: Session -> Result<unit, string>
val setNarration : session: Session -> text: string -> Result<unit, string>
