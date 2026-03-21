/// <summary>Thread-safe cache for the latest simulation state snapshot, used to serve late-joining clients.</summary>
module PhysicsServer.Hub.StateCache

open PhysicsSandbox.Shared.Contracts

/// <summary>Thread-safe container that holds the most recent simulation state behind a lock.</summary>
type StateCache =
    { mutable Current: SimulationState option
      Lock: obj }

/// <summary>Create a new empty state cache with no cached state.</summary>
/// <returns>A fresh StateCache instance.</returns>
let create () =
    { Current = None
      Lock = obj () }

/// <summary>Retrieve the most recently cached simulation state, or None if no state has been received yet.</summary>
/// <param name="cache">The state cache to read from.</param>
/// <returns>The latest SimulationState if available, otherwise None.</returns>
let get (cache: StateCache) =
    lock cache.Lock (fun () -> cache.Current)

/// <summary>Replace the cached state with a new simulation state snapshot.</summary>
/// <param name="cache">The state cache to update.</param>
/// <param name="state">The new simulation state to store.</param>
let update (cache: StateCache) (state: SimulationState) =
    lock cache.Lock (fun () -> cache.Current <- Some state)

/// <summary>Clear the cached state, resetting it to None.</summary>
/// <param name="cache">The state cache to clear.</param>
let clear (cache: StateCache) =
    lock cache.Lock (fun () -> cache.Current <- None)
