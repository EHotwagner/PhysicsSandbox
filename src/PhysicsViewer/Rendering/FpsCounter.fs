namespace PhysicsViewer.Rendering

[<RequireQualifiedAccess>]
module FpsCounter =

    type FpsState =
        { mutable SmoothedFps: float32
          mutable ElapsedSinceLog: float32
          WarningThreshold: float32 }

    let private alpha = 0.1f

    let create (warningThreshold: float32) : FpsState =
        { SmoothedFps = 60.0f
          ElapsedSinceLog = 0.0f
          WarningThreshold = warningThreshold }

    let update (deltaSeconds: float32) (state: FpsState) : float32 =
        let instantFps =
            if deltaSeconds > 1.0f then 0.0f
            elif deltaSeconds > 0.0f then 1.0f / deltaSeconds
            else state.SmoothedFps

        state.SmoothedFps <- alpha * instantFps + (1.0f - alpha) * state.SmoothedFps
        state.ElapsedSinceLog <- state.ElapsedSinceLog + deltaSeconds
        state.SmoothedFps

    let shouldLog (intervalSeconds: float32) (state: FpsState) : bool =
        if state.ElapsedSinceLog >= intervalSeconds then
            state.ElapsedSinceLog <- 0.0f
            true
        else
            false

    let currentFps (state: FpsState) : float32 = state.SmoothedFps

    let isBelowThreshold (state: FpsState) : bool =
        state.SmoothedFps < state.WarningThreshold
