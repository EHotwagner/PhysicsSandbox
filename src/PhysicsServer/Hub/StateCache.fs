/// <summary>Thread-safe caches for late-joining clients: tick state (60 Hz) and property snapshot (on-change).</summary>
module PhysicsServer.Hub.StateCache

open PhysicsSandbox.Shared.Contracts

/// <summary>Thread-safe container that holds the most recent tick state behind a lock.</summary>
type StateCache =
    { mutable Current: TickState option
      Lock: obj }

/// <summary>Thread-safe container that holds the most recent property snapshot behind a lock.</summary>
type PropertyCache =
    { mutable Current: PropertySnapshot option
      Lock: obj }

/// <summary>Create a new empty state cache with no cached state.</summary>
let create () : StateCache =
    { Current = None
      Lock = obj () }

/// <summary>Retrieve the most recently cached tick state, or None if no state has been received yet.</summary>
let get (cache: StateCache) =
    lock cache.Lock (fun () -> cache.Current)

/// <summary>Replace the cached state with a new tick state snapshot.</summary>
let update (cache: StateCache) (state: TickState) =
    lock cache.Lock (fun () -> cache.Current <- Some state)

/// <summary>Clear the cached state, resetting it to None.</summary>
let clear (cache: StateCache) =
    lock cache.Lock (fun () -> cache.Current <- None)

/// <summary>Create a new empty property cache.</summary>
let createPropertyCache () : PropertyCache =
    { Current = None
      Lock = obj () }

/// <summary>Retrieve the most recently cached property snapshot.</summary>
let getProperties (cache: PropertyCache) =
    lock cache.Lock (fun () -> cache.Current)

/// <summary>Replace the cached property snapshot.</summary>
let updateProperties (cache: PropertyCache) (snapshot: PropertySnapshot) =
    lock cache.Lock (fun () -> cache.Current <- Some snapshot)

/// <summary>Clear the property cache.</summary>
let clearProperties (cache: PropertyCache) =
    lock cache.Lock (fun () -> cache.Current <- None)
