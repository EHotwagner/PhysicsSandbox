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

/// Set the running (play/pause) state
val setRunning : World -> bool -> unit

/// Add a rigid body to the world. Returns CommandAck with success/failure.
val addBody : World -> AddBody -> CommandAck

/// Remove a body by identifier. No-op if body does not exist.
val removeBody : World -> string -> CommandAck

/// Add a persistent force to a body (accumulates, applied each step)
val applyForce : World -> string -> Vec3 -> CommandAck

/// Apply a one-shot linear impulse to a body (not stored)
val applyImpulse : World -> string -> Vec3 -> CommandAck

/// Apply a torque to a body (rotational force)
val applyTorque : World -> string -> Vec3 -> CommandAck

/// Remove all persistent forces from a body
val clearForces : World -> string -> CommandAck

/// Set the global gravity vector
val setGravity : World -> Vec3 -> unit

/// Reset the simulation: remove all bodies, clear forces, reset time to 0
val resetSimulation : World -> CommandAck

/// Latest physics tick time in milliseconds (updated each step)
val latestTickMs : unit -> double

/// Latest state serialization time in milliseconds (updated each step)
val latestSerializeMs : unit -> double
