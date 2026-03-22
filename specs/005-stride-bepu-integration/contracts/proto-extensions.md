# Proto Contract Extensions: physics_hub.proto

**Date**: 2026-03-22 | **Branch**: `005-stride-bepu-integration`

All changes are additive and backward-compatible with existing clients.

## New Messages

### Shape Extensions (Shape.oneof fields 4-10)

```protobuf
// Added to Shape.oneof
Capsule capsule = 4;
Cylinder cylinder = 5;
Triangle triangle = 6;
ConvexHull convex_hull = 7;
Compound compound = 8;
MeshShape mesh = 9;
ShapeReference shape_ref = 10;

message Capsule {
  double radius = 1;
  double length = 2;
}

message Cylinder {
  double radius = 1;
  double length = 2;
}

message Triangle {
  Vec3 a = 1;
  Vec3 b = 2;
  Vec3 c = 3;
}

message ConvexHull {
  repeated Vec3 points = 1;
}

message CompoundChild {
  Shape shape = 1;
  Vec3 local_position = 2;
  Vec4 local_orientation = 3;
}

message Compound {
  repeated CompoundChild children = 1;
}

message MeshTriangle {
  Vec3 a = 1;
  Vec3 b = 2;
  Vec3 c = 3;
}

message MeshShape {
  repeated MeshTriangle triangles = 1;
}

message ShapeReference {
  string shape_handle = 1;
}
```

### Shape Registration

```protobuf
message RegisterShape {
  string shape_handle = 1;
  Shape shape = 2;
}

message UnregisterShape {
  string shape_handle = 1;
}
```

### Color and Material

```protobuf
message Color {
  double r = 1;
  double g = 2;
  double b = 3;
  double a = 4;
}

message MaterialProperties {
  double friction = 1;
  double max_recovery_velocity = 2;
  double spring_frequency = 3;
  double spring_damping_ratio = 4;
}
```

### Body Motion Type

```protobuf
enum BodyMotionType {
  DYNAMIC = 0;
  KINEMATIC = 1;
  STATIC = 2;
}
```

### Constraints

```protobuf
message AddConstraint {
  string id = 1;
  string body_a = 2;
  string body_b = 3;
  ConstraintType type = 4;
}

message RemoveConstraint {
  string constraint_id = 1;
}

message ConstraintType {
  oneof constraint {
    BallSocketConstraint ball_socket = 1;
    HingeConstraint hinge = 2;
    WeldConstraint weld = 3;
    DistanceLimitConstraint distance_limit = 4;
    DistanceSpringConstraint distance_spring = 5;
    SwingLimitConstraint swing_limit = 6;
    TwistLimitConstraint twist_limit = 7;
    LinearAxisMotorConstraint linear_axis_motor = 8;
    AngularMotorConstraint angular_motor = 9;
    PointOnLineConstraint point_on_line = 10;
  }
}

message SpringSettings {
  double frequency = 1;
  double damping_ratio = 2;
}

message MotorConfig {
  double max_force = 1;
  double damping = 2;
}

message BallSocketConstraint {
  Vec3 local_offset_a = 1;
  Vec3 local_offset_b = 2;
  SpringSettings spring = 3;
}

message HingeConstraint {
  Vec3 local_hinge_axis_a = 1;
  Vec3 local_hinge_axis_b = 2;
  Vec3 local_offset_a = 3;
  Vec3 local_offset_b = 4;
  SpringSettings spring = 5;
}

message WeldConstraint {
  Vec3 local_offset = 1;
  Vec4 local_orientation = 2;
  SpringSettings spring = 3;
}

message DistanceLimitConstraint {
  Vec3 local_offset_a = 1;
  Vec3 local_offset_b = 2;
  double min_distance = 3;
  double max_distance = 4;
  SpringSettings spring = 5;
}

message DistanceSpringConstraint {
  Vec3 local_offset_a = 1;
  Vec3 local_offset_b = 2;
  double target_distance = 3;
  SpringSettings spring = 4;
}

message SwingLimitConstraint {
  Vec3 axis_local_a = 1;
  Vec3 axis_local_b = 2;
  double max_swing_angle = 3;
  SpringSettings spring = 4;
}

message TwistLimitConstraint {
  Vec3 local_axis_a = 1;
  Vec3 local_axis_b = 2;
  double min_angle = 3;
  double max_angle = 4;
  SpringSettings spring = 5;
}

message LinearAxisMotorConstraint {
  Vec3 local_offset_a = 1;
  Vec3 local_offset_b = 2;
  Vec3 local_axis = 3;
  double target_velocity = 4;
  MotorConfig motor = 5;
}

message AngularMotorConstraint {
  Vec3 target_velocity = 1;
  MotorConfig motor = 2;
}

message PointOnLineConstraint {
  Vec3 local_origin = 1;
  Vec3 local_direction = 2;
  Vec3 local_offset = 3;
  SpringSettings spring = 4;
}

message ConstraintState {
  string id = 1;
  string body_a = 2;
  string body_b = 3;
  ConstraintType type = 4;
}
```

### Physics Queries

```protobuf
message RaycastRequest {
  Vec3 origin = 1;
  Vec3 direction = 2;
  double max_distance = 3;
  bool all_hits = 4;
  uint32 collision_mask = 5;
}

message RayHit {
  string body_id = 1;
  Vec3 position = 2;
  Vec3 normal = 3;
  double distance = 4;
}

message RaycastResponse {
  bool hit = 1;
  repeated RayHit hits = 2;
}

message RaycastBatchRequest {
  repeated RaycastRequest rays = 1;
}

message RaycastBatchResponse {
  repeated RaycastResponse results = 1;
}

message SweepCastRequest {
  Shape shape = 1;
  Vec3 start_position = 2;
  Vec4 orientation = 3;
  Vec3 direction = 4;
  double max_distance = 5;
  uint32 collision_mask = 6;
}

message SweepCastResponse {
  bool hit = 1;
  RayHit closest = 2;
}

message SweepCastBatchRequest {
  repeated SweepCastRequest sweeps = 1;
}

message SweepCastBatchResponse {
  repeated SweepCastResponse results = 1;
}

message OverlapRequest {
  Shape shape = 1;
  Vec3 position = 2;
  Vec4 orientation = 3;
  uint32 collision_mask = 4;
}

message OverlapResponse {
  repeated string body_ids = 1;
}

message OverlapBatchRequest {
  repeated OverlapRequest overlaps = 1;
}

message OverlapBatchResponse {
  repeated OverlapResponse results = 1;
}
```

### Collision Filter Command

```protobuf
message SetCollisionFilter {
  string body_id = 1;
  uint32 collision_group = 2;
  uint32 collision_mask = 3;
}
```

## Modified Messages

### AddBody (new fields 6-12)

```protobuf
message AddBody {
  string id = 1;
  Vec3 position = 2;
  Vec3 velocity = 3;
  double mass = 4;
  Shape shape = 5;
  // new
  MaterialProperties material = 6;
  Color color = 7;
  BodyMotionType motion_type = 8;
  uint32 collision_group = 9;
  uint32 collision_mask = 10;
  Vec3 angular_velocity = 11;
  Vec4 orientation = 12;
}
```

### Body (new fields 9-13)

```protobuf
message Body {
  string id = 1;
  Vec3 position = 2;
  Vec3 velocity = 3;
  double mass = 4;
  Shape shape = 5;
  Vec3 angular_velocity = 6;
  Vec4 orientation = 7;
  bool is_static = 8;
  // new
  Color color = 9;
  BodyMotionType motion_type = 10;
  uint32 collision_group = 11;
  uint32 collision_mask = 12;
  MaterialProperties material = 13;
}
```

### SimulationState (new field 6)

```protobuf
message SimulationState {
  repeated Body bodies = 1;
  double time = 2;
  bool running = 3;
  double tick_ms = 4;
  double serialize_ms = 5;
  // new
  repeated ConstraintState constraints = 6;
  repeated RegisteredShapeState registered_shapes = 7;
}

message RegisteredShapeState {
  string shape_handle = 1;
  Shape shape = 2;
}
```

### SimulationCommand (new oneof fields 11-15)

```protobuf
message SimulationCommand {
  oneof command {
    // existing 1-10
    AddConstraint add_constraint = 11;
    RemoveConstraint remove_constraint = 12;
    RegisterShape register_shape = 13;
    UnregisterShape unregister_shape = 14;
    SetCollisionFilter set_collision_filter = 15;
  }
}
```

## New RPCs on PhysicsHub

```protobuf
service PhysicsHub {
  // existing RPCs unchanged
  rpc Raycast (RaycastRequest) returns (RaycastResponse);
  rpc RaycastBatch (RaycastBatchRequest) returns (RaycastBatchResponse);
  rpc SweepCast (SweepCastRequest) returns (SweepCastResponse);
  rpc SweepCastBatch (SweepCastBatchRequest) returns (SweepCastBatchResponse);
  rpc Overlap (OverlapRequest) returns (OverlapResponse);
  rpc OverlapBatch (OverlapBatchRequest) returns (OverlapBatchResponse);
}
```

## Query Routing Design

Queries need a request-response path from server to simulation. Current `SimulationLink` is command-only (no response channel). Design:

- Server maintains a bounded channel for query requests with `TaskCompletionSource<TResponse>` callbacks
- `SimulationLink` proto extended with a new bidirectional query stream, or queries are handled via a separate internal mechanism
- Alternative: Add query RPCs to `SimulationLink` service (simulation acts as both client and server)
- Recommended: Extend `SimulationLink` with query-specific RPCs that the server calls on the simulation
