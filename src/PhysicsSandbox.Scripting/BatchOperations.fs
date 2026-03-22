module PhysicsSandbox.Scripting.BatchOperations

open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Scripting.Helpers

let batchAdd (s: Session) (commands: SimulationCommand list) =
    let chunks = commands |> List.chunkBySize 100
    for chunk in chunks do
        let response = batchCommands s chunk |> ok
        let failures = response.Results |> Seq.filter (fun r -> not r.Success) |> Seq.toList
        if failures.Length > 0 then
            for f in failures do
                printfn "  [BATCH FAIL] command %d: %s" f.Index f.Message
