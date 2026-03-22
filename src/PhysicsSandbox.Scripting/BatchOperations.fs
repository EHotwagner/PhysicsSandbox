/// <summary>Batch command execution with automatic chunking and failure reporting.</summary>
module PhysicsSandbox.Scripting.BatchOperations

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Scripting.Helpers

/// <summary>Sends a list of simulation commands in batches of 100, logging any failures.</summary>
/// <param name="s">Active session connected to the physics server.</param>
/// <param name="commands">List of commands to send. Automatically split into chunks of 100.</param>
/// <remarks>
/// The server enforces a maximum of 100 commands per batch request.
/// This function handles chunking transparently and reports per-command failures
/// with their index and error message.
/// </remarks>
let batchAdd (s: Session) (commands: SimulationCommand list) =
    let chunks = commands |> List.chunkBySize 100
    for chunk in chunks do
        let response = batchCommands s chunk |> ok
        let failures = response.Results |> Seq.filter (fun r -> not r.Success) |> Seq.toList
        if failures.Length > 0 then
            for f in failures do
                printfn "  [BATCH FAIL] command %d: %s" f.Index f.Message
