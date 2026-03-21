# Research: Python Demo Scripts

**Feature**: 004-python-demo-scripts
**Date**: 2026-03-21

## R1: Python gRPC Client Library Choice

**Decision**: Use `grpcio` + `grpcio-tools` + `protobuf` (the official Google gRPC Python packages).

**Rationale**: These are the standard, well-maintained Python gRPC packages. They support all features needed: unary RPCs (SendCommand, SendBatchCommand), server-streaming RPCs (StreamState), and proto code generation. The alternative (`grpclib` — asyncio-native) would add complexity with no benefit for synchronous demo scripts.

**Alternatives considered**:
- `grpclib` (async gRPC): Rejected — demos are sequential/synchronous; async would add unnecessary complexity.
- HTTP REST wrapper: Rejected — server only exposes gRPC, no REST API exists.

## R2: Proto Stub Generation Strategy

**Decision**: Provide a `generate_stubs.sh` script that runs `grpc_tools.protoc` to generate Python stubs from the existing `physics_hub.proto` file. Generated stubs go into `demos_py/generated/`.

**Rationale**: Python gRPC requires generated `_pb2.py` (messages) and `_pb2_grpc.py` (service stubs) files. Generating these from the same proto file used by the .NET services ensures contract alignment. A script makes regeneration easy after proto changes.

**Alternatives considered**:
- Checking generated stubs into git: Acceptable and recommended for convenience — avoids requiring `grpcio-tools` at runtime. The generation script serves as the source-of-truth for regeneration.
- Runtime proto compilation: Rejected — adds startup complexity and dependency on `grpcio-tools` at runtime.

## R3: Session/Connection Model in Python

**Decision**: Wrap `grpc.insecure_channel` in a simple `Session` dataclass holding the channel and the `PhysicsHubStub`. No background state streaming — demos don't need it (the F# demos don't use cached state either; they use it only in `launch`/`stop` which can query state on-demand).

**Rationale**: The F# PhysicsClient has a complex session model (background StreamState, exponential backoff, body registry) because it's a reusable library. The Python demos are scripts — they need connect, send commands, disconnect. Simplicity is the priority.

**Key simplifications vs F# PhysicsClient**:
- No background StreamState task (demos don't read state continuously)
- No body registry (demos track IDs locally when needed)
- No exponential backoff reconnection (scripts run once and exit)
- State queries done on-demand via `get_state()` helper when needed

## R4: Prelude Module Design — Mapping F# Helpers to Python

**Decision**: `prelude.py` provides all helpers as module-level functions, matching the F# Prelude pattern.

| F# Helper | Python Equivalent | Notes |
|-----------|-------------------|-------|
| `connect` / `disconnect` | `connect(addr)` / `disconnect(session)` | Returns Session dataclass |
| `ok` | Not needed | Python uses exceptions, not Result types |
| `sleep(ms)` | `sleep(ms)` | Wraps `time.sleep(ms/1000)` |
| `runFor(s, seconds)` | `run_for(session, seconds)` | play → sleep → pause |
| `toVec3(x,y,z)` | `to_vec3(x,y,z)` | Constructs proto Vec3 |
| `resetSimulation(s)` | `reset_simulation(session)` | pause → reset → add_plane → set_gravity |
| `nextId(prefix)` | `next_id(prefix)` | Global counter dict |
| `makeSphereCmd` | `make_sphere_cmd(id, pos, radius, mass)` | Returns SimulationCommand |
| `makeBoxCmd` | `make_box_cmd(id, pos, half_extents, mass)` | Returns SimulationCommand |
| `makeImpulseCmd` | `make_impulse_cmd(body_id, impulse)` | Returns SimulationCommand |
| `makeTorqueCmd` | `make_torque_cmd(body_id, torque)` | Returns SimulationCommand |
| `timed(label, fn)` | `timed(label)` context manager | Pythonic: `with timed("label"):` |
| `batchAdd(s, cmds)` | `batch_add(session, cmds)` | Auto-chunks at 100 |

**Additional helpers needed** (used by F# demos but provided by PhysicsClient library, not Prelude):
- `play(session)` / `pause(session)` — send PlayPause command
- `set_camera(session, pos, target)` — send SetCamera view command
- `set_gravity(session, gravity)` — send SetGravity command
- `wireframe(session, enabled)` — send ToggleWireframe command
- `list_bodies(session)` — print body table from StreamState snapshot
- `status(session)` — print simulation status summary
- `add_plane(session, normal, id)` — add static plane
- `bowling_ball(session, pos, mass, id)` — preset body
- `boulder(session, pos, mass, id)` — preset body
- `stack(session, count, pos)` — generator
- `pyramid(session, count, pos)` — generator
- `row(session, count, pos)` — generator
- `grid(session, rows, cols, pos)` — generator
- `random_spheres(session, count, seed)` — generator
- `push(session, body_id, direction, magnitude)` — steering
- `launch(session, body_id, target, speed)` — steering

All of these map directly to gRPC calls using the proto stubs.

## R5: Error Handling Strategy

**Decision**: Use Python exceptions instead of F# Result types. Demo scripts catch and display errors; runner scripts catch per-demo exceptions for pass/fail reporting.

**Rationale**: Python idiom is exceptions, not Result monads. The F# `ok` helper that unwraps Results with `failwith` on error is effectively the same as Python's exception model. Keeping Python-native patterns makes scripts readable for Python developers.

## R6: Standalone Demo Execution Pattern

**Decision**: Each demo script has a `name`, `description`, and `run(session)` function at module level, plus an `if __name__ == "__main__"` block for standalone execution that handles connection/disconnection.

**Rationale**: Mirrors the F# pattern where each `DemoNN_Name.fsx` can be loaded individually. The `__main__` guard enables both standalone execution (`python demo01_hello_drop.py`) and import by runners (`from demo01_hello_drop import name, description, run`).

## R7: Direction Enum for Steering

**Decision**: Use a Python `enum.Enum` for `Direction` (Up, Down, North, South, East, West) mapping to impulse vectors, matching the F# `Direction` discriminated union.

**Rationale**: Direct translation of the F# steering API. The enum maps each direction to a unit vector used by `push()`.
