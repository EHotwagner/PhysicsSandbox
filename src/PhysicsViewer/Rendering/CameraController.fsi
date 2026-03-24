module PhysicsViewer.CameraController

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Input
open PhysicsSandbox.Shared.Contracts

/// Discriminated union representing active camera behavior modes.
type CameraMode =
    | Transitioning of startPos: Vector3 * startTarget: Vector3 * startZoom: float * endPos: Vector3 * endTarget: Vector3 * endZoom: float * elapsed: float32 * duration: float32
    | LookingAt of bodyId: string * startTarget: Vector3 * elapsed: float32 * duration: float32
    | Following of bodyId: string
    | Orbiting of bodyId: string * startAngle: float32 * totalDegrees: float32 * radius: float32 * height: float32 * elapsed: float32 * duration: float32
    | Chasing of bodyId: string * offset: Vector3
    | Framing of bodyIds: string list
    | Shaking of basePosition: Vector3 * baseTarget: Vector3 * intensity: float32 * elapsed: float32 * duration: float32

/// Opaque camera state.
type CameraState

/// Create default camera state (position, target, up, zoom).
val defaultCamera: unit -> CameraState

/// Apply a SetCamera command (overrides current position/target/up).
val applySetCamera: SetCamera -> CameraState -> CameraState

/// Apply a SetZoom command.
val applySetZoom: SetZoom -> CameraState -> CameraState

/// Apply interactive mouse/keyboard input (orbit, pan, zoom).
/// dt is the frame delta time in seconds.
val applyInput: InputManager -> float32 -> CameraState -> CameraState

/// Smoothstep easing: 3t^2 - 2t^3 (clamped to [0,1]).
val smoothstep: float32 -> float32

/// Cancel any active camera mode, keeping current interpolated position.
val cancelMode: CameraState -> CameraState

/// Advance the active camera mode by dt seconds using body positions for lookups.
val updateCameraMode: float32 -> Map<string, Vector3> -> CameraState -> CameraState

/// Returns true if a camera mode is currently active.
val isActive: CameraState -> bool

/// Set the active camera mode.
val setMode: CameraMode -> CameraState -> CameraState

/// Apply camera state to a Stride camera entity's transform.
val applyToCamera: CameraState -> Entity -> unit

/// Get the camera position.
val position: CameraState -> Vector3

/// Get the camera target.
val target: CameraState -> Vector3

/// Get the zoom level.
val zoomLevel: CameraState -> float
