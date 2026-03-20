module PhysicsViewer.SceneManager

open Stride.Engine
open Stride.Games
open PhysicsSandbox.Shared.Contracts

/// Shape kind discriminated union for color mapping.
type ShapeKind =
    | Sphere
    | Box
    | Unknown

/// Opaque handle to the scene manager's internal state.
type SceneState

/// Create initial scene state.
val create: unit -> SceneState

/// Classify a proto Shape into a ShapeKind.
val classifyShape: Shape -> ShapeKind

/// Apply a simulation state snapshot to the scene.
/// Adds new entities, updates existing positions/orientations, removes absent bodies.
val applyState: Game -> Scene -> SceneState -> SimulationState -> SceneState

/// Apply wireframe toggle. Returns updated SceneState with new wireframe mode.
val applyWireframe: Game -> ToggleWireframe -> SceneState -> SceneState

/// Get current wireframe mode.
val isWireframe: SceneState -> bool

/// Get the current simulation time from the last applied state.
val simulationTime: SceneState -> float

/// Get whether the simulation is running from the last applied state.
val isRunning: SceneState -> bool
