namespace PhysicsViewer.Rendering

[<RequireQualifiedAccess>]
module FpsCounter =

    type FpsState

    val create : warningThreshold: float32 -> FpsState
    val update : deltaSeconds: float32 -> state: FpsState -> float32
    val shouldLog : intervalSeconds: float32 -> state: FpsState -> bool
    val currentFps : state: FpsState -> float32
    val isBelowThreshold : state: FpsState -> bool
