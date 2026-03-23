module PhysicsServer.Hub.StateCache

open PhysicsSandbox.Shared.Contracts

/// Opaque handle to a thread-safe tick state cache.
type StateCache

/// Opaque handle to a thread-safe property snapshot cache.
type PropertyCache

/// Create a new empty state cache.
val create: unit -> StateCache

/// Get the most recently cached tick state, or None if no state has been received.
val get: StateCache -> TickState option

/// Update the cache with a new tick state snapshot.
val update: StateCache -> TickState -> unit

/// Clear the cached state.
val clear: StateCache -> unit

/// Create a new empty property cache.
val createPropertyCache: unit -> PropertyCache

/// Get the most recently cached property snapshot.
val getProperties: PropertyCache -> PropertySnapshot option

/// Update the property cache with a new snapshot.
val updateProperties: PropertyCache -> PropertySnapshot -> unit

/// Clear the property cache.
val clearProperties: PropertyCache -> unit
