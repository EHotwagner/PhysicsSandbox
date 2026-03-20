# Proto Contract Extensions: Physics Simulation Service

**Branch**: `002-physics-simulation` | **Date**: 2026-03-20
**File**: `src/PhysicsSandbox.Shared.Contracts/Protos/physics_hub.proto`

## New Message Types

```protobuf
message Vec4 {
  double x = 1;
  double y = 2;
  double z = 3;
  double w = 4;
}

message RemoveBody {
  string body_id = 1;
}

message ApplyImpulse {
  string body_id = 1;
  Vec3 impulse = 2;
}

message ApplyTorque {
  string body_id = 1;
  Vec3 torque = 2;
}

message ClearForces {
  string body_id = 1;
}
```

## Extended Messages

### SimulationCommand — new oneof variants

```protobuf
message SimulationCommand {
  oneof command {
    AddBody add_body = 1;        // existing
    ApplyForce apply_force = 2;  // existing
    SetGravity set_gravity = 3;  // existing
    StepSimulation step = 4;     // existing
    PlayPause play_pause = 5;    // existing
    RemoveBody remove_body = 6;        // NEW
    ApplyImpulse apply_impulse = 7;    // NEW
    ApplyTorque apply_torque = 8;      // NEW
    ClearForces clear_forces = 9;      // NEW
  }
}
```

### Body — new fields

```protobuf
message Body {
  string id = 1;          // existing
  Vec3 position = 2;      // existing
  Vec3 velocity = 3;      // existing
  double mass = 4;        // existing
  Shape shape = 5;        // existing
  Vec3 angular_velocity = 6;  // NEW
  Vec4 orientation = 7;       // NEW (quaternion: x, y, z, w)
}
```

## Unchanged Services

Both `PhysicsHub` and `SimulationLink` service definitions remain unchanged. The new command types flow through the existing `SimulationCommand` oneof. The extended `Body` fields flow through the existing `SimulationState` message.

## Backward Compatibility

- All new fields use higher field numbers than existing ones
- New oneof variants are additive (proto3 ignores unknown fields)
- Existing compiled clients/servers continue to work; new fields are default-valued
- The PhysicsServer hub routes `SimulationCommand` opaquely — it does not inspect the oneof, so it forwards new variants without changes
