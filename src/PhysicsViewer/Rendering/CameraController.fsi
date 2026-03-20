module PhysicsViewer.CameraController

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Input
open PhysicsSandbox.Shared.Contracts

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

/// Apply camera state to a Stride camera entity's transform.
val applyToCamera: CameraState -> Entity -> unit

/// Get the camera position.
val position: CameraState -> Vector3

/// Get the camera target.
val target: CameraState -> Vector3

/// Get the zoom level.
val zoomLevel: CameraState -> float
