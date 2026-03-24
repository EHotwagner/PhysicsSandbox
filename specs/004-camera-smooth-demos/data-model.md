# Data Model: 004-camera-smooth-demos

**Date**: 2026-03-24

## Entities

### CameraState (viewer-side, extended)

Existing fields (unchanged):
- `Position: Vector3` — World-space eye position
- `Target: Vector3` — World-space look-at target
- `Up: Vector3` — Orientation vector (typically Y-up)
- `ZoomLevel: float` — Zoom multiplier (1.0 = default)

New fields:
- `ActiveMode: CameraMode option` — Currently active camera behavior mode. `None` = idle (manual control).

### CameraMode (viewer-side, new discriminated union)

| Variant | Fields | Description |
|---------|--------|-------------|
| Transitioning | startPos, startTarget, startZoom, endPos, endTarget, endZoom, elapsed, duration | Smooth interpolation from A→B |
| LookingAt | bodyId, startPos, startTarget, elapsed, duration | One-shot smooth orient toward body |
| Following | bodyId | Continuous per-frame tracking (camera target = body position) |
| Orbiting | bodyId, startAngle, totalDegrees, elapsed, duration | Revolve around body over time |
| Chasing | bodyId, offset | Continuous per-frame tracking with fixed offset |
| Framing | bodyIds (string list) | Continuous per-frame auto-position to keep all bodies visible |
| Shaking | basePosition, baseTarget, intensity, elapsed, duration | Additive random offset to current camera |

### State Transitions

```
Idle ──[SmoothCamera cmd]──► Transitioning
Idle ──[CameraLookAt cmd]──► LookingAt
Idle ──[CameraFollow cmd]──► Following
Idle ──[CameraOrbit cmd]──► Orbiting
Idle ──[CameraChase cmd]──► Chasing
Idle ──[CameraFrameBodies]──► Framing
Any  ──[CameraShake cmd]──► Shaking (captures current state as base)

Transitioning ──[duration elapsed]──► Idle (at end position)
LookingAt ──[duration elapsed]──► Idle (facing body)
Orbiting ──[duration elapsed]──► Idle (at final orbit position)
Shaking ──[duration elapsed]──► Idle (restored to base)

Any ──[new camera cmd]──► Cancel current, start new
Any ──[mouse input]──► Cancel current, Idle
Any ──[CameraStop cmd]──► Idle (hold current position)
```

### SceneState (viewer-side, extended)

Existing fields (unchanged):
- `Bodies: Map<string, Entity>`
- `Placeholders: Set<string>`
- `SimTime: float`
- `SimRunning: bool`
- `Wireframe: bool`
- `DemoName: string option`
- `DemoDescription: string option`

New fields:
- `NarrationText: string option` — Current narration label text. `None` = no label shown.

### Narration Label (viewer-side)

- Single string displayed at fixed screen position (10, 50)
- Set via `SetNarration` ViewCommand (text field)
- Cleared by sending empty text
- Rendered each frame via `DebugTextSystem.Print()` when `Some text`
- Independent of demo metadata overlay (which is at (10, 10))

### Body Position Map (viewer-side, transient)

- `Map<string, Vector3>` — Built each frame from `latestSimState.Bodies`
- Used by body-relative camera modes to resolve body ID → world position
- Not persisted — rebuilt from simulation state each frame

## Validation Rules

- `duration` in camera commands must be ≥ 0. Duration of 0 = instant snap.
- `bodyId` in body-relative commands must be a non-empty string. Invalid IDs result in mode cancellation.
- `degrees` in orbit defaults to 360 if omitted or 0.
- `intensity` in shake must be > 0. Duration must be > 0.
- `bodyIds` in frameBodies must contain at least 1 ID.
- `offset` in chase is a Vec3 relative offset from the body.
