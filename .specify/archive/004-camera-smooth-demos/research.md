# Research: 004-camera-smooth-demos

**Date**: 2026-03-24

## R1: Smooth Camera Interpolation in Stride3D

**Decision**: Frame-based interpolation using delta time in the viewer's update loop. Smoothstep easing (cubic Hermite: `3t² - 2t³`).

**Rationale**: Stride3D has no built-in tween/animation system. The viewer already has per-frame `dt = float32 time.Elapsed.TotalSeconds` in its update loop. The existing FPS counter uses exponential moving average (`α * instant + (1-α) * smooth`) which validates the pattern. Smoothstep is simple, produces natural-looking ease-in-out, and requires no external dependencies.

**Alternatives considered**:
- Linear interpolation: Rejected — looks robotic, no ease-in/out.
- Exponential smoothing (like FPS counter): Rejected — doesn't have a defined completion time, would overshoot/undershoot target.
- External tween library: Rejected — unnecessary dependency for a single use case.

## R2: Body Position Lookup in Viewer

**Decision**: Build a `Map<string, Vector3>` body position lookup from `latestSimState.Bodies` each frame, similar to the existing pattern in `DebugRenderer.updateConstraints`.

**Rationale**: The viewer already reconstructs a full `SimulationState` from TickState (dynamic bodies, 60Hz) + `bodyPropsCache` (static bodies, on-change). The DebugRenderer already builds `bodyPositions = simState.Bodies |> Seq.map (fun b -> b.Id, protoVec3ToStride b.Position) |> Map.ofSeq`. This exact pattern can be reused for body-relative camera modes. Body IDs are strings matching between client and viewer.

**Alternatives considered**:
- Query server for body position: Rejected — adds latency, viewer already has the data.
- Use Entity.Transform.Position from SceneState.Bodies: Possible but depends on scene update timing; using SimulationState directly is more reliable.

## R3: ViewCommand Proto Extension Strategy

**Decision**: Add new ViewCommand oneof variants starting at field 5. Use separate messages for each camera mode rather than a single polymorphic message.

**Rationale**: The ViewCommand oneof currently uses fields 1-4 (SetCamera, ToggleWireframe, SetZoom, SetDemoMetadata). Adding new variants is backward-compatible — old viewers ignore unknown fields. Separate messages are clearer than a single "CameraCommand" with nested oneof. The server forwards ViewCommands unchanged (no server code changes needed).

**New proto messages planned**:
- `SmoothCamera` (field 5): position, target, up, duration_seconds, zoom_level
- `CameraLookAt` (field 6): body_id, duration_seconds
- `CameraFollow` (field 7): body_id
- `CameraOrbit` (field 8): body_id, duration_seconds, degrees
- `CameraChase` (field 9): body_id, offset
- `CameraFrameBodies` (field 10): repeated body_ids
- `CameraShake` (field 11): intensity, duration_seconds
- `CameraStop` (field 12): (empty — cancels active mode)
- `SetNarration` (field 13): text

**Alternatives considered**:
- Extend existing SetCamera with optional duration: Rejected — mixes concerns, doesn't cover body-relative modes.
- Single CameraCommand with nested oneof: Rejected — extra nesting adds complexity for no benefit.
- Reuse SetDemoMetadata for narration: Rejected — different purpose and screen position.

## R4: Narration Label Rendering

**Decision**: Use Stride's `DebugTextSystem.Print()` at a fixed screen position below the existing demo label (10, 10) and status bar (10, 30). Narration at (10, 50).

**Rationale**: The viewer already uses `DebugTextSystem.Print()` for the demo label overlay and FPS/status bar. This is the simplest approach — no custom UI framework needed. Text is rendered each frame (stateless), so updating is just changing the stored string.

**Alternatives considered**:
- Stride UI framework (SpriteFont + TextBlock): More control over styling but significantly more complex setup. Overkill for a single text label.
- Semi-transparent backdrop rectangle: Could be added later but `DebugTextSystem` already renders readable text. Start simple.

## R5: Camera State Machine Architecture

**Decision**: Extend `CameraState` with an optional `ActiveMode` discriminated union that tracks the current camera behavior (idle, transitioning, following, orbiting, chasing, framing, shaking).

**Rationale**: The camera needs to know what it's doing each frame to update correctly. A DU cleanly separates mode-specific state (e.g., orbit needs angle progress, follow needs body ID, shake needs offset generator) while keeping the core position/target/up/zoom always valid. Mode cancellation is simply setting `ActiveMode = None`.

**State structure**:
```
CameraState:
  Position: Vector3
  Target: Vector3
  Up: Vector3
  ZoomLevel: float
  ActiveMode: CameraMode option

CameraMode (DU):
  | Transitioning of start * target * elapsed * duration
  | LookingAt of bodyId * elapsed * duration
  | Following of bodyId
  | Orbiting of bodyId * startAngle * degrees * elapsed * duration
  | Chasing of bodyId * offset
  | Framing of bodyIds
  | Shaking of baseState * intensity * elapsed * duration
```

**Alternatives considered**:
- Separate mutable fields for each mode: Rejected — hard to ensure only one mode is active.
- External state machine library: Rejected — unnecessary dependency for a simple state.

## R6: Script-Side Smooth Camera Helpers

**Decision**: Add fire-and-forget helper functions to both Prelude.fsx and prelude.py. Smooth moves are asynchronous (script sends command and continues; viewer handles interpolation). Scripts use `sleep` to wait for transitions to complete.

**Rationale**: The existing pattern is fire-and-forget: `setCamera s pos target |> ignore`. Smooth moves follow the same pattern but with a duration. The script doesn't need to know when the transition completes — it sleeps for the duration and sends the next command. This avoids bidirectional communication complexity.

**F# naming**: `smoothCamera`, `lookAtBody`, `followBody`, `orbitBody`, `chaseBody`, `frameBodies`, `shakeCamera`, `stopCamera`, `setNarration`, `clearNarration`
**Python naming**: `smooth_camera`, `look_at_body`, `follow_body`, `orbit_body`, `chase_body`, `frame_bodies`, `shake_camera`, `stop_camera`, `set_narration`, `clear_narration`

**Alternatives considered**:
- Blocking/awaiting helpers that wait for transition to complete: Rejected — requires bidirectional callback, overcomplicates the fire-and-forget architecture.
- Single `camera()` function with mode parameter: Rejected — less discoverable than separate named functions.
