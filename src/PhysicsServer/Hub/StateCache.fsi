module PhysicsServer.Hub.StateCache

open PhysicsSandbox.Shared.Contracts

/// Opaque handle to a thread-safe state cache.
type StateCache

/// Create a new empty state cache.
val create: unit -> StateCache

/// Get the most recently cached simulation state, or None if no state has been received.
val get: StateCache -> SimulationState option

/// Update the cache with a new simulation state snapshot.
val update: StateCache -> SimulationState -> unit

/// Clear the cached state.
val clear: StateCache -> unit
