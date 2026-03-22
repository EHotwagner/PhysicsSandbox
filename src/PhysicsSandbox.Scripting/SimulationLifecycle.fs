/// <summary>High-level simulation lifecycle control: reset, run, and ID generation.</summary>
module PhysicsSandbox.Scripting.SimulationLifecycle

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsSandbox.Scripting.Helpers

/// <summary>Fully resets the simulation to a clean state with a ground plane and standard gravity.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <remarks>
/// Performs these steps in order: pause → server-side reset (with fallback to manual clearAll) →
/// reset ID generator → add ground plane → set gravity to (0, -9.81, 0) → sleep 100ms for stabilization.
/// </remarks>
let resetSimulation (s: Session) =
    pause s |> ignore
    try
        reset s |> ok
    with ex ->
        printfn "  [RESET ERROR] %s — falling back to manual clear" ex.Message
        clearAll s |> ignore
    PhysicsClient.IdGenerator.reset ()
    addPlane s None None |> ignore
    setGravity s (0.0, -9.81, 0.0) |> ignore
    sleep 100

/// <summary>Runs the simulation for the specified duration, then pauses.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <param name="seconds">How long to run in seconds.</param>
let runFor (s: Session) (seconds: float) =
    play s |> ignore
    sleep (int (seconds * 1000.0))
    pause s |> ignore

/// <summary>Generates the next sequential ID for a body with the given shape prefix.</summary>
/// <param name="prefix">Shape kind prefix (e.g., <c>"sphere"</c>, <c>"box"</c>).</param>
/// <returns>A human-readable ID like <c>"sphere-1"</c> or <c>"box-3"</c>.</returns>
let nextId prefix = PhysicsClient.IdGenerator.nextId prefix
