# Contract: ViewCommand Proto Extensions

**Date**: 2026-03-24
**File**: `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`

## Existing ViewCommand (unchanged)

```protobuf
message ViewCommand {
  oneof command {
    SetCamera set_camera = 1;           // Instant camera position (existing)
    ToggleWireframe toggle_wireframe = 2; // Wireframe toggle (existing)
    SetZoom set_zoom = 3;               // Instant zoom (existing)
    SetDemoMetadata set_demo_metadata = 4; // Demo overlay (existing)
    // NEW fields below
    SmoothCamera smooth_camera = 5;
    CameraLookAt camera_look_at = 6;
    CameraFollow camera_follow = 7;
    CameraOrbit camera_orbit = 8;
    CameraChase camera_chase = 9;
    CameraFrameBodies camera_frame_bodies = 10;
    CameraShake camera_shake = 11;
    CameraStop camera_stop = 12;
    SetNarration set_narration = 13;
  }
}
```

## New Messages

```protobuf
// Smooth interpolation from current camera state to target over duration
message SmoothCamera {
  Vec3 position = 1;
  Vec3 target = 2;
  Vec3 up = 3;
  double duration_seconds = 4;
  double zoom_level = 5;  // 0 = keep current zoom
}

// Smoothly orient camera to face a body
message CameraLookAt {
  string body_id = 1;
  double duration_seconds = 2;  // 0 = instant
}

// Continuously track a body (camera target follows body each frame)
message CameraFollow {
  string body_id = 1;
}

// Revolve camera around a body over specified duration
message CameraOrbit {
  string body_id = 1;
  double duration_seconds = 2;
  double degrees = 3;  // 0 = full 360°
}

// Continuously follow a body with fixed relative offset
message CameraChase {
  string body_id = 1;
  Vec3 offset = 2;
}

// Auto-position camera to keep all listed bodies in view
message CameraFrameBodies {
  repeated string body_ids = 1;
}

// Apply camera shake effect
message CameraShake {
  double intensity = 1;
  double duration_seconds = 2;
}

// Cancel any active camera mode (hold current position)
message CameraStop {
}

// Set or clear the narration label overlay
message SetNarration {
  string text = 1;  // Empty string = clear narration
}
```

## Client API Contract (PhysicsClient ViewCommands)

### F# Signatures (ViewCommands.fsi)

```fsharp
// Existing (unchanged)
val setCamera : session: Session -> position: (float * float * float) -> target: (float * float * float) -> Result<unit, string>
val setZoom : session: Session -> level: float -> Result<unit, string>
val wireframe : session: Session -> enabled: bool -> Result<unit, string>
val setDemoMetadata : session: Session -> name: string -> description: string -> Result<unit, string>

// New smooth camera
val smoothCamera : session: Session -> position: (float * float * float) -> target: (float * float * float) -> durationSeconds: float -> Result<unit, string>
val smoothCameraWithZoom : session: Session -> position: (float * float * float) -> target: (float * float * float) -> durationSeconds: float -> zoomLevel: float -> Result<unit, string>

// New body-relative modes
val cameraLookAt : session: Session -> bodyId: string -> durationSeconds: float -> Result<unit, string>
val cameraFollow : session: Session -> bodyId: string -> Result<unit, string>
val cameraOrbit : session: Session -> bodyId: string -> durationSeconds: float -> degrees: float -> Result<unit, string>
val cameraChase : session: Session -> bodyId: string -> offset: (float * float * float) -> Result<unit, string>
val cameraFrameBodies : session: Session -> bodyIds: string list -> Result<unit, string>
val cameraShake : session: Session -> intensity: float -> durationSeconds: float -> Result<unit, string>
val cameraStop : session: Session -> Result<unit, string>

// New narration
val setNarration : session: Session -> text: string -> Result<unit, string>
```

## Scripting API Contract

### F# Prelude Helpers

```fsharp
// Camera
val smoothCamera : Session -> (float*float*float) -> (float*float*float) -> float -> unit
val lookAtBody : Session -> string -> float -> unit
val followBody : Session -> string -> unit
val orbitBody : Session -> string -> float -> float -> unit
val chaseBody : Session -> string -> (float*float*float) -> unit
val frameBodies : Session -> string list -> unit
val shakeCamera : Session -> float -> float -> unit
val stopCamera : Session -> unit

// Narration
val setNarration : Session -> string -> unit
val clearNarration : Session -> unit
```

### Python Prelude Helpers

```python
def smooth_camera(session, position, target, duration_seconds) -> None
def look_at_body(session, body_id, duration_seconds) -> None
def follow_body(session, body_id) -> None
def orbit_body(session, body_id, duration_seconds, degrees=360.0) -> None
def chase_body(session, body_id, offset) -> None
def frame_bodies(session, body_ids) -> None
def shake_camera(session, intensity, duration_seconds) -> None
def stop_camera(session) -> None

def set_narration(session, text) -> None
def clear_narration(session) -> None
```
