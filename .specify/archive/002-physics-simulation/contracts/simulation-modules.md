# F# Module Contracts: Physics Simulation Service

**Branch**: `002-physics-simulation` | **Date**: 2026-03-20

## PhysicsSimulation Project — Public Module Signatures

### Module: SimulationWorld

Manages the physics world lifecycle and state.

```fsharp
// SimulationWorld.fsi
module PhysicsSimulation.SimulationWorld

open PhysicsSandbox.Shared.Contracts

/// Opaque simulation world handle
type World

/// Create a new simulation world with default configuration (paused, zero gravity)
val create : unit -> World

/// Destroy the simulation world and release resources
val destroy : World -> unit

/// Get whether the simulation is currently running (playing)
val isRunning : World -> bool

/// Get the current simulation time
val time : World -> double

/// Step the simulation by one fixed time step. Returns the new state.
val step : World -> SimulationState

/// Get the current state as a proto SimulationState (without stepping)
val currentState : World -> SimulationState

// --- Body Management (US2) ---

/// Add a rigid body to the world. Returns CommandAck with success/failure.
/// Rejects duplicate IDs and zero/negative mass.
val addBody : World -> AddBody -> CommandAck

/// Remove a body by identifier. No-op if body does not exist.
val removeBody : World -> string -> CommandAck

// --- Force Application (US3) ---

/// Add a persistent force to a body (accumulates, applied each step)
val applyForce : World -> string -> Vec3 -> CommandAck

/// Apply a one-shot linear impulse to a body (not stored)
val applyImpulse : World -> string -> Vec3 -> CommandAck

/// Apply a torque to a body (rotational force)
val applyTorque : World -> string -> Vec3 -> CommandAck

/// Remove all persistent forces from a body
val clearForces : World -> string -> CommandAck

// --- Gravity (US4) ---

/// Set the global gravity vector
val setGravity : World -> Vec3 -> unit

// --- Lifecycle (US1) ---

/// Set the running (play/pause) state
val setRunning : World -> bool -> unit
```

### Module: CommandHandler

Processes simulation commands against the world.

```fsharp
// CommandHandler.fsi
module PhysicsSimulation.CommandHandler

open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld

/// Process a simulation command against the world.
/// Returns a CommandAck indicating success or failure.
val handle : World -> SimulationCommand -> CommandAck
```

### Module: SimulationClient

Connects to the server and runs the simulation loop.

```fsharp
// SimulationClient.fsi
module PhysicsSimulation.SimulationClient

open System.Threading

/// Run the simulation client. Connects to the server via SimulationLink,
/// processes commands, and streams state. Blocks until cancellation or
/// server disconnection.
val run : serverAddress: string -> CancellationToken -> Async<unit>
```

## Module Relationships

```
Program.fs (entry point)
  └── SimulationClient.run (connects to server, runs loop)
        ├── SimulationWorld.create/step/destroy (physics lifecycle)
        └── CommandHandler.handle (command dispatch)
```

## Constitution Compliance

- Every public module has a `.fsi` signature file (Principle V)
- Surface-area baseline tests validate the public API (Principle V)
- Modules communicate only via proto messages at the gRPC boundary (Principle I, II)
- BepuFSharp dependency is internal to SimulationWorld; not exposed in public API (Principle III)
