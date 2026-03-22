# Research: Stride BepuPhysics Integration

**Date**: 2026-03-22 | **Branch**: `005-stride-bepu-integration`

## Decision 1: Debug Visualization Approach

**Decision**: Use Stride's lower-level `WireFrameRenderObject` + `SinglePassWireframeRenderFeature` for custom debug wireframes, rather than the `DebugRenderProcessor`.

**Rationale**: The `DebugRenderProcessor` in Stride.BepuPhysics.Debug is tightly coupled to `CollidableComponent` — it subscribes to `CollidableProcessor` and reads poses from native Bepu components. The viewer receives physics state via gRPC and creates plain visual entities (no `BodyComponent`/`StaticComponent`). The Debug package's processor cannot track these entities.

However, the lower-level `WireFrameRenderObject` and `SinglePassWireframeRenderFeature` classes are public. The viewer can create `WireFrameRenderObject` instances from its own shape geometry data, add them to the visibility group, and get high-quality wireframe rendering without needing native Bepu components.

**Alternatives considered**:
- **Add native Bepu components to viewer entities** — Rejected. Would run a second BepuPhysics simulation inside Stride, contradicting the distributed architecture.
- **Use existing flat-material wireframe toggle** — Rejected. Not true wireframe rendering; just flat-shaded materials. Missing constraint visualization entirely.
- **Use DebugRenderProcessor directly** — Rejected. Requires CollidableComponent on every entity; fundamentally incompatible with gRPC-driven viewer.

## Decision 2: Stride Package Strategy

**Decision**: No new package references needed. `Stride.BepuPhysics` and `Stride.BepuPhysics.Debug` are already transitive dependencies via `Stride.CommunityToolkit.Bepu 1.0.0-preview.62`.

**Rationale**: The PhysicsViewer fsproj already references `Stride.CommunityToolkit.Bepu`, which transitively pulls in `Stride.BepuPhysics 4.3.0.2507` and `Stride.BepuPhysics.Debug 4.3.0.2507`. All packages target .NET 10.0 and are version-aligned.

**Note**: There is a minor BepuPhysics2 version divergence: viewer resolves `2.5.0-beta.25` (via Stride), simulation resolves `2.5.0-beta.28` (via BepuFSharp). No conflict since they are in separate projects communicating via gRPC.

## Decision 3: BepuFSharp Wrapper Scope

**Decision**: Extend BepuFSharp with Stride.BepuPhysics type interop, sweep casts, overlap queries, constraint readback, and runtime filter/material modification.

**Rationale**: BepuFSharp already has:
- 10 constraint types (BallSocket, Hinge, Weld, DistanceLimit, DistanceSpring, SwingLimit, TwistLimit, LinearAxisMotor, AngularMotor, PointOnLine)
- Constraint add/remove
- Single-hit and all-hit raycasting
- Per-body collision filtering (group/mask)
- Per-body material properties with per-pair blending
- Contact events

Missing (to add):
- Sweep casts (`ISweepHitHandler` wrapping `Simulation.Sweep`)
- AABB overlap queries (`BroadPhase.GetOverlaps`)
- Constraint readback (`Solver.GetDescription` for serialization)
- Batched raycasting (`SimulationRayBatcher`)
- Ray/sweep collision mask filtering (AllowTest callbacks currently always return true)
- Runtime filter/material modification after body creation
- Stride.BepuPhysics type conversion module (new dependency)

## Decision 4: Constraint Scope Alignment

**Decision**: Use BepuFSharp's existing 10 constraint types (which already cover the spec's 9 curated types).

**Rationale**: The spec's curated list maps 1:1 to BepuFSharp's existing `ConstraintDesc` union:
| Spec Name | BepuFSharp ConstraintDesc |
|-----------|--------------------------|
| Hinge | Hinge |
| Ball-socket | BallSocket |
| Distance (min/max) | DistanceLimit |
| Weld | Weld |
| Angular motor | AngularMotor |
| Linear motor | LinearAxisMotor |
| Swing limit | SwingLimit |
| Twist limit | TwistLimit |
| Point-on-line servo | PointOnLine |

BepuFSharp also has `DistanceSpring` (10th type) — include it as a bonus.

## Decision 5: Material Property Naming

**Decision**: Use BepuPhysics2-native terminology: `friction`, `max_recovery_velocity`, `spring_frequency`, `spring_damping_ratio`.

**Rationale**: BepuPhysics2 does NOT use traditional coefficient of restitution. Instead:
- `MaxRecoveryVelocity` = max penetration recovery speed (higher = more bounce)
- `SpringFrequency` = contact spring stiffness (Hz)
- `SpringDampingRatio` = 1.0 = critically damped (no bounce), < 1.0 = bouncy

Using engine-native terms avoids impedance mismatch and keeps the API honest. The spec's "bounciness" concept maps to `max_recovery_velocity`.

## Decision 6: Proto Backward Compatibility

**Decision**: All proto changes are additive — new oneof fields, new message types, new RPCs. No existing fields modified.

**Rationale**: Proto3 oneof extensions and new RPCs are backward-compatible. Old clients ignore unknown fields. New enum `BodyMotionType` defaults to `DYNAMIC` (0), preserving existing behavior. `Body.is_static` retained alongside new `motion_type` for old consumer compatibility.

## Decision 7: Shape Caching Mechanism

**Decision**: Client-named string handles for shape registration. Server stores `Map<string, ShapeId>`. Shape definitions sent in state stream on first use for late-joining clients.

**Rationale**: String handles because the client is the authority on naming. Server never generates shape IDs. `UnregisterShape` available for cleanup. Shapes cleared on simulation reset. Late-joining clients receive all registered shape definitions on initial state stream connection.

## Decision 8: Query RPC Design

**Decision**: Dedicated typed RPCs (`Raycast`, `SweepCast`, `Overlap`) on PhysicsHub, not tunneled through `SendCommand`.

**Rationale**: Queries are request-response by nature. `SendCommand` returns only `CommandAck` (no result data). Dedicated RPCs give typed responses. Batch variant for `Raycast` only (most common). Sweep/overlap batch can be added later.

**Note**: Queries route through the server to the simulation, requiring a new internal mechanism (likely a request channel with response callback, since `SimulationLink` is currently command-only with no response path).
