/// <summary>Batch command execution with automatic chunking and failure reporting.</summary>
module PhysicsSandbox.Scripting.BatchOperations

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Scripting.Helpers

/// <summary>Aggregated result of a batch operation across all chunks.</summary>
type BatchResult =
    { Succeeded: int
      Failed: (int * string) list }

/// <summary>Sends a list of simulation commands in batches of 100, logging any failures to the console.</summary>
/// <param name="s">Active session connected to the physics server (obtained via <c>connect "http://localhost:5180" |> ok</c>).</param>
/// <param name="commands">List of <see cref="T:PhysicsSandbox.Shared.Contracts.SimulationCommand"/> to send.
/// Can be any length — automatically split into chunks of 100 (the server-enforced maximum per batch).
/// Typical sizes: 1–50 for interactive use, 100–500 for stress tests. Lists over 500 commands work
/// but take proportionally longer (one round-trip per 100 commands).</param>
/// <returns>A <see cref="BatchResult"/> summarizing how many commands succeeded and which failed.</returns>
/// <remarks>
/// Each batch is sent as a single gRPC call. Per-command failures are logged to the console
/// with the format <c>  [BATCH FAIL] command N: error message</c>. Common failure reasons:
/// duplicate body ID, unknown body ID for impulse/torque commands, or invalid shape parameters.
/// </remarks>
/// <example>
/// <code>
/// // Create 200 spheres in 2 automatic batches
/// let cmds = [ for i in 1..200 ->
///     makeSphereCmd (nextId "sphere") (0.0, float i * 0.5, 0.0) 0.3 1.0 ]
/// let result = batchAdd session cmds
/// printfn "Created %d bodies, %d failures" result.Succeeded result.Failed.Length
///
/// // Mix body creation and forces in one batch
/// let setup = [
///     makeSphereCmd "ball" (0.0, 5.0, 0.0) 0.5 1.0
///     makeImpulseCmd "ball" (0.0, 10.0, 0.0)
/// ]
/// batchAdd session setup |> ignore
/// </code>
/// </example>
let batchAdd (s: Session) (commands: SimulationCommand list) : BatchResult =
    let mutable totalSucceeded = 0
    let mutable allFailed = []
    let mutable globalOffset = 0
    let chunks = commands |> List.chunkBySize 100
    for chunk in chunks do
        let response = batchCommands s chunk |> ok
        for r in response.Results do
            if r.Success then
                totalSucceeded <- totalSucceeded + 1
            else
                allFailed <- (globalOffset + r.Index, r.Message) :: allFailed
                printfn "  [BATCH FAIL] command %d: %s" (globalOffset + r.Index) r.Message
        globalOffset <- globalOffset + chunk.Length
    { Succeeded = totalSucceeded; Failed = List.rev allFailed }
