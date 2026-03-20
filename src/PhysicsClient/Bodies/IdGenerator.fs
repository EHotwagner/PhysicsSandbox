module PhysicsClient.IdGenerator

open System.Collections.Concurrent

let private counters = ConcurrentDictionary<string, int>()

let nextId (shapeKind: string) : string =
    let value = counters.AddOrUpdate(shapeKind, 1, fun _ current -> current + 1)
    $"{shapeKind}-{value}"

let reset () : unit =
    counters.Clear()
