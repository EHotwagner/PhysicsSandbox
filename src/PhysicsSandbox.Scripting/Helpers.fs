/// <summary>General-purpose utility functions for error handling, timing, and thread control.</summary>
module PhysicsSandbox.Scripting.Helpers

/// <summary>Unwraps an <c>Ok</c> value from a <c>Result</c>, throwing an exception on <c>Error</c>.</summary>
/// <param name="r">The result to unwrap.</param>
/// <returns>The contained value if <c>Ok</c>.</returns>
/// <exception cref="T:System.Exception">Thrown with the error message when the result is <c>Error</c>.</exception>
/// <example>
/// <code>
/// let id = connect "http://localhost:5180" |> ok   // returns Session on success, throws on failure
/// let count = clearAll session |> ok                // returns cleared body count
/// </code>
/// </example>
/// <remarks>
/// Use this to unwrap <c>Result&lt;'a, string&gt;</c> values returned by PhysicsClient functions.
/// All PhysicsClient operations (connect, addSphere, play, etc.) return Results — <c>ok</c> converts
/// them to direct values for scripting convenience. In production code, prefer pattern matching.
/// </remarks>
let ok r = r |> Result.defaultWith (fun e -> failwith e)

/// <summary>Pauses the current thread for the specified number of milliseconds.</summary>
/// <param name="ms">Duration to sleep in milliseconds. Typical values: 100 (stabilization pause),
/// 500 (short delay between actions), 1000–5000 (observation window).</param>
/// <example>
/// <code>
/// sleep 100    // brief pause for simulation to stabilize after reset
/// sleep 2000   // 2-second pause to observe physics
/// </code>
/// </example>
let sleep (ms: int) = System.Threading.Thread.Sleep(ms)

/// <summary>Executes a function, measures its wall-clock duration, and prints the elapsed time to the console.</summary>
/// <param name="label">A descriptive label printed alongside the timing output.
/// Use short, identifying names like <c>"add bodies"</c>, <c>"simulation run"</c>, <c>"batch 200 spheres"</c>.</param>
/// <param name="f">The function to execute and time. Takes <c>unit</c> and returns any value.</param>
/// <returns>The return value of <paramref name="f"/>, unmodified.</returns>
/// <remarks>Output format: <c>  [TIME] label: N ms</c>. Useful for benchmarking command
/// throughput and identifying performance bottlenecks in demo scripts.</remarks>
/// <example>
/// <code>
/// let bodies = timed "create 100 spheres" (fun () ->
///     [ for i in 1..100 -> makeSphereCmd (nextId "sphere") (0.0, float i, 0.0) 0.3 1.0 ])
/// // prints:   [TIME] create 100 spheres: 2 ms
/// </code>
/// </example>
let timed (label: string) (f: unit -> 'a) : 'a =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let result = f ()
    sw.Stop()
    printfn "  [TIME] %s: %d ms" label sw.ElapsedMilliseconds
    result
