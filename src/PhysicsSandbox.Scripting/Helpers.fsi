/// <summary>General-purpose utility functions for error handling, timing, and thread control.</summary>
module PhysicsSandbox.Scripting.Helpers

/// <summary>Unwraps an <c>Ok</c> value from a <c>Result</c>, throwing an exception on <c>Error</c>.</summary>
/// <param name="r">The result to unwrap.</param>
/// <returns>The contained value if <c>Ok</c>.</returns>
/// <exception cref="T:System.Exception">Thrown with the error message when the result is <c>Error</c>.</exception>
/// <example>
/// <code>
/// let s = connect "http://localhost:5180" |> ok   // throws on connection failure
/// let count = clearAll session |> ok               // returns cleared body count
/// </code>
/// </example>
/// <remarks>
/// All PhysicsClient operations return <c>Result&lt;'a, string&gt;</c>. Use <c>ok</c> for scripting
/// convenience; prefer pattern matching in production code.
/// </remarks>
val ok : Result<'a, string> -> 'a

/// <summary>Pauses the current thread for the specified number of milliseconds.</summary>
/// <param name="ms">Duration to sleep in milliseconds. Typical values: 100 (stabilization pause),
/// 500 (short delay), 1000–5000 (observation window).</param>
val sleep : int -> unit

/// <summary>Executes a function, measures its wall-clock duration, and prints the elapsed time to the console.</summary>
/// <param name="label">A descriptive label like <c>"add bodies"</c> or <c>"batch 200 spheres"</c>.</param>
/// <param name="f">The function to execute and time.</param>
/// <returns>The return value of <paramref name="f"/>.</returns>
/// <remarks>Output format: <c>  [TIME] label: N ms</c>. Useful for benchmarking command throughput.</remarks>
/// <example>
/// <code>
/// let bodies = timed "create 100 spheres" (fun () ->
///     [ for i in 1..100 -> makeSphereCmd (nextId "sphere") (0.0, float i, 0.0) 0.3 1.0 ])
/// </code>
/// </example>
val timed : string -> (unit -> 'a) -> 'a
