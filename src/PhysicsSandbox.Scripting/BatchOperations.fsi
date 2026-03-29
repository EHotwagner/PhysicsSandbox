/// <summary>Batch command execution with automatic chunking and failure reporting.</summary>
module PhysicsSandbox.Scripting.BatchOperations

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts

/// <summary>Aggregated result of a batch operation across all chunks.</summary>
type BatchResult =
    { Succeeded: int
      Failed: (int * string) list }

/// <summary>Sends a list of simulation commands in batches of 100, logging any failures to the console.</summary>
/// <param name="s">Active session connected to the physics server (from <c>connect url |> ok</c>).</param>
/// <param name="commands">List of SimulationCommands to send. Any length — auto-chunked at 100
/// (the server-enforced maximum per batch). Typical: 1–50 interactive, 100–500 stress tests.</param>
/// <returns>A <see cref="BatchResult"/> summarizing how many commands succeeded and which failed.</returns>
/// <remarks>
/// Per-command failures are logged as <c>  [BATCH FAIL] command N: error message</c>.
/// Common failures: duplicate body ID, unknown body ID for impulse/torque, invalid shape.
/// </remarks>
/// <example>
/// <code>
/// let cmds = [ for i in 1..200 ->
///     makeSphereCmd (nextId "sphere") (0.0, float i * 0.5, 0.0) 0.3 1.0 ]
/// let result = batchAdd session cmds   // sent in 2 automatic batches of 100
/// printfn "%d succeeded, %d failed" result.Succeeded result.Failed.Length
/// </code>
/// </example>
val batchAdd : Session -> SimulationCommand list -> BatchResult
