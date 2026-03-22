/// <summary>High-level simulation lifecycle control: reset, run, and ID generation.</summary>
module PhysicsSandbox.Scripting.SimulationLifecycle

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsSandbox.Scripting.Helpers

/// <summary>Fully resets the simulation to a clean state with a ground plane and standard gravity.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <remarks>
/// Performs these steps in order:
/// <list type="number">
/// <item>Pause the simulation</item>
/// <item>Server-side reset (falls back to manual <c>clearAll</c> if reset RPC fails)</item>
/// <item>Reset the ID generator so <c>nextId</c> counters restart from 1</item>
/// <item>Add a ground plane at Y=0</item>
/// <item>Set gravity to Earth standard: (0, -9.81, 0)</item>
/// <item>Sleep 100ms for the simulation to stabilize</item>
/// </list>
/// Call this at the start of every demo or experiment to ensure a predictable starting state.
/// </remarks>
/// <example>
/// <code>
/// let s = connect "http://localhost:5180" |> ok
/// resetSimulation s    // clean slate — empty world with ground and gravity
/// // Now add bodies and run...
/// </code>
/// </example>
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

/// <summary>Runs the simulation for the specified duration, then pauses automatically.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <param name="seconds">How long to run in seconds. Typical values: 1.0–2.0 for quick drops,
/// 3.0–5.0 for observing bouncing or rolling, 10.0+ for long-running scenarios.
/// The simulation runs at 60 Hz fixed timestep, so 3.0 seconds = 180 physics steps.</param>
/// <example>
/// <code>
/// resetSimulation s
/// batchAdd s [makeSphereCmd "ball" (0.0, 10.0, 0.0) 0.5 1.0]
/// runFor s 3.0    // watch the ball fall and bounce for 3 seconds
/// </code>
/// </example>
let runFor (s: Session) (seconds: float) =
    play s |> ignore
    sleep (int (seconds * 1000.0))
    pause s |> ignore

/// <summary>Generates the next sequential ID for a body with the given shape prefix.</summary>
/// <param name="prefix">Shape kind prefix. Common values: <c>"sphere"</c>, <c>"box"</c>, <c>"ball"</c>,
/// <c>"crate"</c>, <c>"wall"</c>, <c>"domino"</c>. Any string works — the generator maintains
/// a separate counter per prefix.</param>
/// <returns>A human-readable ID combining the prefix and a sequential number, e.g., <c>"sphere-1"</c>,
/// <c>"sphere-2"</c>, <c>"box-1"</c>. Counters reset to 0 when <c>resetSimulation</c> is called.</returns>
/// <example>
/// <code>
/// let id1 = nextId "sphere"   // "sphere-1"
/// let id2 = nextId "sphere"   // "sphere-2"
/// let id3 = nextId "box"      // "box-1"
/// resetSimulation s           // resets counters
/// let id4 = nextId "sphere"   // "sphere-1" (counter restarted)
/// </code>
/// </example>
let nextId prefix = PhysicsClient.IdGenerator.nextId prefix
