# Research: 003-3d-viewer

## R1: 3D Rendering — Stride3D with Community Toolkit (Code-Only)

**Decision**: Use Stride3D via the Community Toolkit code-only workflow. No Stride Game Studio required. Reference implementation at `/home/developer/tools/Stride3DSkill/`.

**Rationale**: User directive. The Community Toolkit provides `Game.Run()`, `Create3DPrimitive()`, `AddGraphicsCompositor()`, `Add3DCamera()`, `Add3DGround()` etc. as extension methods that work from pure F# code. The reference implementation demonstrates all needed patterns: scene building, entity creation, input handling, materials, and physics helpers.

**Key F# interop constraints** (from stride3d-fsharp skill):
- `Vector3` arithmetic: cannot use `+`/`*` operators in F# — use `Vector3.Add(&a, &b, &result)` or helper functions
- Extension methods: open the namespace; fall back to static `GameExtensions.*` calls if unresolved
- `Nullable<Vector3>`: construct explicitly with `System.Nullable<Vector3>(v)`

**NuGet packages**: `Stride.CommunityToolkit`, `Stride.CommunityToolkit.Bepu`, `Stride.CommunityToolkit.Skyboxes`, `Stride.CommunityToolkit.Linux` (all `1.0.0-preview.62`)

**Build**: `dotnet build -p:StrideCompilerSkipBuild=true` (skip asset compiler for dev/CI)

**Graphics API**: OpenGL (`<StrideGraphicsApi>OpenGL</StrideGraphicsApi>`) for container/GPU-passthrough compatibility.

**Alternatives considered**:
- Avalonia + HelixToolkit: 2D framework with 3D addon, weaker physics viz support
- Raw OpenTK: too low-level for the scope of this feature
- Web-based (Three.js): would break the F#/.NET-only constitution

## R2: gRPC Approach — Extend Existing Proto Contract

**Decision**: Extend `physics_hub.proto` with a new `StreamViewCommands` RPC on `PhysicsHub` service. Use `/fsgrpc-client` skill patterns for the gRPC client implementation. Use `/fsgrpc-proto` skill patterns for proto extension.

**Rationale**: The existing proto has `SendViewCommand` (client → server) and a `ViewCommandChannel` in the MessageRouter that buffers commands, but no mechanism for the server to forward ViewCommands to the viewer. A server-streaming RPC mirrors the existing `StreamState` pattern and is the simplest extension.

**Proto addition**:
```protobuf
// In PhysicsHub service:
rpc StreamViewCommands (StateRequest) returns (stream ViewCommand);
```

**Server-side changes required**:
- `MessageRouter.fsi`: add `readViewCommand: MessageRouter -> CancellationToken -> Task<ViewCommand option>`
- `PhysicsHubService`: implement `StreamViewCommands` override (reads from ViewCommandChannel, streams to caller)

**Alternatives considered**:
- Embed ViewCommand in SimulationState: mixes concerns, bloats state messages
- Bidirectional stream: overkill — viewer only receives view commands, never sends them
- Polling: defeats real-time purpose

## R3: Threading Model — Game Loop + Background gRPC

**Decision**: Run Stride3D `Game.Run()` on the main thread (required for graphics). Run gRPC streaming and the .NET host on background threads. Marshal data via `ConcurrentQueue<T>`.

**Rationale**: Stride3D requires the main thread for its game loop and windowing. gRPC streams are async I/O that work naturally on background tasks. A `ConcurrentQueue` is lock-free and suitable for the producer-consumer pattern between gRPC callbacks and the game update loop.

**Pattern**:
```
Main thread:     Host.StartAsync() → Game.Run(start, update) → Host.StopAsync()
Background:      gRPC StreamState → ConcurrentQueue<SimulationState>
Background:      gRPC StreamViewCommands → ConcurrentQueue<ViewCommand>
Update loop:     drain queues → update Stride entities + camera
```

**Alternatives considered**:
- Run game in a hosted service: Game.Run() must be on the main thread; hosted services run on thread pool
- Channel<T> instead of ConcurrentQueue: provides backpressure but adds complexity; viewer always wants latest state, not queued history

## R4: Testing Strategy — Pure Logic + Aspire Integration

**Decision**: Unit test pure logic modules (SceneManager mapping, CameraController math) without GPU. Integration test gRPC connectivity via Aspire.

**Rationale**: Stride3D entity creation requires a running Game instance with GPU. But the mapping logic (SimulationState → entity descriptors, ViewCommand → camera state) is pure and testable. The stride3d-fsharp skill reference tests demonstrate this pattern: test the data/builder layer, not the GPU rendering.

**Alternatives considered**:
- GPU-in-CI: fragile, requires GPU passthrough in CI containers
- Mock Stride Game: complex and brittle; test the logic instead

## R5: Observability — ServiceDefaults with Background Host

**Decision**: Use `AddServiceDefaults()` on a background `WebApplication` host for structured logging, OpenTelemetry, and health checks. The game loop runs on the main thread independently.

**Rationale**: Constitution Principle VII requires ServiceDefaults. The viewer needs service discovery env vars (injected by Aspire). Running a minimal web host on a background thread provides `/health` and `/alive` endpoints for Aspire dashboard monitoring without conflicting with Stride3D's main-thread requirement.

**Alternatives considered**:
- Skip ServiceDefaults entirely: violates constitution
- ILogger without host: loses health checks and Aspire dashboard integration

## R6: Body-to-Entity Mapping

**Decision**: Maintain a `Map<string, Entity>` keyed by body ID. On each state update, diff against current map: add new entities, update positions/orientations of existing ones, remove entities no longer present.

**Rationale**: The simulation streams full state snapshots (all bodies). A diff approach avoids recreating entities every frame, which would be expensive. Stride entities are mutable (Transform.Position, Transform.Rotation are settable), so in-place updates are natural.

**Shape mapping**:
- Proto `Sphere` → `PrimitiveModelType.Sphere` (blue)
- Proto `Box` → `PrimitiveModelType.Cube` (orange)
- Proto `Plane` → `PrimitiveModelType.Plane` or large flat cube (grey) — fallback
- Unknown → `PrimitiveModelType.Sphere` (red) — fallback
