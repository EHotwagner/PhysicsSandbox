module PhysicsViewer.SceneManager

open Stride.Engine
open Stride.Games
open PhysicsSandbox.Shared.Contracts

/// Opaque handle to the scene manager's internal state.
type SceneState

/// Create initial scene state.
val create: unit -> SceneState

/// Apply a simulation state snapshot to the scene.
/// Adds new entities, updates existing positions/orientations, removes absent bodies.
/// Optional MeshResolver resolves CachedShapeRef to real shapes; unresolved shapes render as bounding box placeholders.
val applyState: Game -> Scene -> SceneState -> SimulationState -> Streaming.MeshResolver.MeshResolverState option -> SceneState

/// Apply wireframe toggle. Returns updated SceneState with new wireframe mode.
val applyWireframe: Game -> ToggleWireframe -> SceneState -> SceneState

/// Get current wireframe mode.
val isWireframe: SceneState -> bool

/// Get the current simulation time from the last applied state.
val simulationTime: SceneState -> float

/// Get whether the simulation is running from the last applied state.
val isRunning: SceneState -> bool

/// Apply demo metadata from a SetDemoMetadata view command.
val applyDemoMetadata: SetDemoMetadata -> SceneState -> SceneState

/// Get the current demo name.
val demoName: SceneState -> string option

/// Get the current demo description.
val demoDescription: SceneState -> string option
