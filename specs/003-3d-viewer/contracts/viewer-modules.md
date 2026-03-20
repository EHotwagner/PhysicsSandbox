# Viewer Module Contracts (.fsi Signatures)

## Module: PhysicsViewer.SceneManager

Manages the mapping between simulation state and Stride3D entities.

```fsharp
module PhysicsViewer.SceneManager

open System.Collections.Concurrent
open Stride.Engine
open Stride.Games
open PhysicsSandbox.Shared.Contracts

/// Opaque handle to the scene manager's internal state.
type SceneState

/// Shape kind discriminated union for color mapping.
type ShapeKind =
    | Sphere
    | Box
    | Unknown

/// Create initial scene state.
val create: unit -> SceneState

/// Classify a proto Shape into a ShapeKind.
val classifyShape: Shape -> ShapeKind

/// Apply a simulation state snapshot to the scene.
/// Adds new entities, updates existing positions/orientations, removes absent bodies.
val applyState: Game -> Scene -> SceneState -> SimulationState -> SceneState

/// Apply wireframe toggle. Returns updated SceneState with new wireframe mode.
val applyWireframe: ToggleWireframe -> SceneState -> SceneState

/// Get current wireframe mode.
val isWireframe: SceneState -> bool

/// Get the current simulation time from the last applied state.
val simulationTime: SceneState -> float

/// Get whether the simulation is running from the last applied state.
val isRunning: SceneState -> bool
```

## Module: PhysicsViewer.CameraController

Manages camera state from both REPL commands and interactive input.

```fsharp
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
val applyInput: InputManager -> float32 -> CameraState -> CameraState

/// Apply camera state to a Stride CameraComponent's entity transform.
val applyToCamera: CameraState -> Entity -> unit
```

## Module: PhysicsViewer.ViewerClient

gRPC client that streams simulation state and view commands from the server.

```fsharp
module PhysicsViewer.ViewerClient

open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open PhysicsSandbox.Shared.Contracts

/// Start streaming simulation state from the server.
/// Enqueues received states into the provided queue.
val streamState:
    serverAddress: string ->
    stateQueue: ConcurrentQueue<SimulationState> ->
    ct: CancellationToken ->
    Task<unit>

/// Start streaming view commands from the server.
/// Enqueues received commands into the provided queue.
val streamViewCommands:
    serverAddress: string ->
    commandQueue: ConcurrentQueue<ViewCommand> ->
    ct: CancellationToken ->
    Task<unit>
```
