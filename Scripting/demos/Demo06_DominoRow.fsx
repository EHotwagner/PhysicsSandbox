// Demo 06: Domino Row
// A line of tall bricks set up as dominoes, toppled by a push.
// Usage: dotnet fsi Scripting/demos/Demo06_DominoRow.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Steering
open PhysicsClient.StateDisplay

module Demo06 =
    let name = "Domino Row"
    let description = "A row of 20 brick dominoes toppled by a push."

    let run (s: Session) =
        resetSimulation s

        // Camera: side view along the row
        setCamera s (-2.0, 3.0, 6.0) (5.0, 0.5, 0.0) |> ignore
        setDemoInfo s "Demo 06: Domino Row" "Classic domino chain reaction — one push topples them all."

        // Batch-create 20 dominoes — pre-generate IDs for push reference
        let ids = [ for _ in 0..19 -> nextId "box" ]
        let cmds =
            [ for i in 0..19 do
                let x = float i * 0.5
                makeBoxCmd ids.[i] (x, 0.3, 0.0) (0.05, 0.3, 0.15) 1.0 ]
        batchAdd s cmds
        let firstId = ids.[0]

        printfn "  Placed 20 dominoes in a row"

        // Let them settle standing
        runFor s 1.0

        // Push the first domino
        printfn "  Toppling first domino..."
        push s firstId East 3.0 |> ignore

        // Track the cascade with camera
        setCamera s (0.0, 2.5, 4.0) (2.0, 0.3, 0.0) |> ignore
        runFor s 2.0
        setCamera s (3.0, 2.5, 4.0) (5.0, 0.3, 0.0) |> ignore
        runFor s 2.0
        setCamera s (6.0, 2.5, 4.0) (8.0, 0.3, 0.0) |> ignore
        runFor s 2.0

        // Pan camera to the end
        setCamera s (12.0, 2.0, 4.0) (9.0, 0.0, 0.0) |> ignore
        sleep 500
        listBodies s

runStandalone Demo06.name Demo06.run
