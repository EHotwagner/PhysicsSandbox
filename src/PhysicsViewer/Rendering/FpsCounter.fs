namespace PhysicsViewer.Rendering

/// <summary>
/// Provides exponentially-smoothed FPS tracking with configurable warning thresholds and periodic logging support.
/// </summary>
[<RequireQualifiedAccess>]
module FpsCounter =

    /// <summary>
    /// Mutable state for the FPS counter, tracking the smoothed frame rate and elapsed time since last log.
    /// </summary>
    type FpsState =
        { /// <summary>The exponentially-smoothed frames-per-second value.</summary>
          mutable SmoothedFps: float32
          /// <summary>Accumulated seconds since the last log interval check.</summary>
          mutable ElapsedSinceLog: float32
          /// <summary>FPS threshold below which performance is considered degraded.</summary>
          WarningThreshold: float32 }

    let private alpha = 0.1f

    /// <summary>
    /// Creates a new FPS counter state with the given warning threshold, initialized to 60 FPS.
    /// </summary>
    /// <param name="warningThreshold">The FPS value below which performance warnings should trigger.</param>
    /// <returns>A new FpsState ready for frame updates.</returns>
    let create (warningThreshold: float32) : FpsState =
        { SmoothedFps = 60.0f
          ElapsedSinceLog = 0.0f
          WarningThreshold = warningThreshold }

    /// <summary>
    /// Updates the smoothed FPS value using exponential moving average and accumulates elapsed time for logging.
    /// </summary>
    /// <param name="deltaSeconds">The time elapsed since the last frame in seconds.</param>
    /// <param name="state">The FPS state to update (mutated in place).</param>
    /// <returns>The updated smoothed FPS value.</returns>
    let update (deltaSeconds: float32) (state: FpsState) : float32 =
        let instantFps =
            if deltaSeconds > 1.0f then 0.0f
            elif deltaSeconds > 0.0f then 1.0f / deltaSeconds
            else state.SmoothedFps

        state.SmoothedFps <- alpha * instantFps + (1.0f - alpha) * state.SmoothedFps
        state.ElapsedSinceLog <- state.ElapsedSinceLog + deltaSeconds
        state.SmoothedFps

    /// <summary>
    /// Checks whether enough time has elapsed to emit a periodic log entry, resetting the timer if so.
    /// </summary>
    /// <param name="intervalSeconds">The minimum seconds between log entries.</param>
    /// <param name="state">The FPS state whose elapsed timer is checked and reset.</param>
    /// <returns>True if the log interval has been reached; false otherwise.</returns>
    let shouldLog (intervalSeconds: float32) (state: FpsState) : bool =
        if state.ElapsedSinceLog >= intervalSeconds then
            state.ElapsedSinceLog <- 0.0f
            true
        else
            false

    /// <summary>Gets the current smoothed FPS value.</summary>
    let currentFps (state: FpsState) : float32 = state.SmoothedFps

    /// <summary>Returns true if the smoothed FPS is below the configured warning threshold.</summary>
    let isBelowThreshold (state: FpsState) : bool =
        state.SmoothedFps < state.WarningThreshold
