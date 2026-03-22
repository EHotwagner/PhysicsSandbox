module PhysicsViewer.DebugRenderer

open Stride.Engine
open Stride.Games
open PhysicsSandbox.Shared.Contracts

/// Opaque debug renderer state.
type DebugState

/// Create initial debug renderer state (disabled).
val create: unit -> DebugState

/// Update debug shapes from simulation state (creates/updates/removes wireframe entities).
val updateShapes: Game -> Scene -> DebugState -> SimulationState -> DebugState

/// Update constraint visualization lines.
val updateConstraints: Game -> Scene -> DebugState -> SimulationState -> DebugState

/// Toggle debug visualization on/off.
val setEnabled: bool -> DebugState -> DebugState

/// Get whether debug visualization is enabled.
val isEnabled: DebugState -> bool
