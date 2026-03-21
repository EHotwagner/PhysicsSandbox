/// <summary>Generates sequential human-readable IDs for physics bodies, namespaced by shape kind.</summary>
module PhysicsClient.IdGenerator

open System.Collections.Concurrent

let private counters = ConcurrentDictionary<string, int>()

/// <summary>Generates the next human-readable ID for the given shape kind (e.g., "sphere" produces "sphere-1", "sphere-2", etc.).</summary>
/// <param name="shapeKind">The shape category used as the ID prefix (e.g., "sphere", "box", "plane").</param>
/// <returns>A unique string ID combining the shape kind and an incrementing counter.</returns>
let nextId (shapeKind: string) : string =
    let value = counters.AddOrUpdate(shapeKind, 1, fun _ current -> current + 1)
    $"{shapeKind}-{value}"

/// <summary>Resets all ID counters back to zero. Typically called when reconnecting or clearing simulation state.</summary>
let reset () : unit =
    counters.Clear()
