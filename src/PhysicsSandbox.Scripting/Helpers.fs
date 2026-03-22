/// <summary>General-purpose utility functions for error handling, timing, and thread control.</summary>
module PhysicsSandbox.Scripting.Helpers

/// <summary>Unwraps an <c>Ok</c> value from a <c>Result</c>, throwing an exception on <c>Error</c>.</summary>
/// <param name="r">The result to unwrap.</param>
/// <returns>The contained value if <c>Ok</c>.</returns>
/// <exception cref="T:System.Exception">Thrown with the error message when the result is <c>Error</c>.</exception>
let ok r = r |> Result.defaultWith (fun e -> failwith e)

/// <summary>Pauses the current thread for the specified number of milliseconds.</summary>
/// <param name="ms">Duration to sleep in milliseconds.</param>
let sleep (ms: int) = System.Threading.Thread.Sleep(ms)

/// <summary>Executes a function, measures its wall-clock duration, and prints the elapsed time to the console.</summary>
/// <param name="label">A descriptive label printed alongside the timing output.</param>
/// <param name="f">The function to execute and time.</param>
/// <returns>The return value of <paramref name="f"/>.</returns>
/// <remarks>Output format: <c>  [TIME] label: N ms</c></remarks>
let timed (label: string) (f: unit -> 'a) : 'a =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let result = f ()
    sw.Stop()
    printfn "  [TIME] %s: %d ms" label sw.ElapsedMilliseconds
    result
