# Proto Contract Changes: Demo Metadata

**File**: `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`

## New Message

```proto
message SetDemoMetadata {
  string name = 1;
  string description = 2;
}
```

## Extended Message

```proto
message ViewCommand {
  oneof command {
    SetCamera set_camera = 1;
    ToggleWireframe toggle_wireframe = 2;
    SetZoom set_zoom = 3;
    SetDemoMetadata set_demo_metadata = 4;  // NEW
  }
}
```

## Impact

- **No new RPCs**: Uses existing `SendViewCommand` and `StreamViewCommands`
- **No breaking changes**: New oneof variant is additive (field 4)
- **Server**: No code changes — ViewCommand forwarding is payload-agnostic
- **Viewer**: Must handle new variant in ViewCommand processing (Program.fs lines 322-331)
- **Clients**: PhysicsClient ViewCommands module needs new builder function
- **Python**: Proto regeneration provides new message type automatically
