module PhysicsServer.Hub.StateCache

open PhysicsSandbox.Shared.Contracts

type StateCache =
    { mutable Current: SimulationState option
      Lock: obj }

let create () =
    { Current = None
      Lock = obj () }

let get (cache: StateCache) =
    lock cache.Lock (fun () -> cache.Current)

let update (cache: StateCache) (state: SimulationState) =
    lock cache.Lock (fun () -> cache.Current <- Some state)

let clear (cache: StateCache) =
    lock cache.Lock (fun () -> cache.Current <- None)
