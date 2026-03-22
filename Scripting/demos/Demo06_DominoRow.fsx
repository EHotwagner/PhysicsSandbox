// Demo 06: Domino Row
// A line of tall bricks set up as dominoes, toppled by a push.

module Demo06 =
    let name = "Domino Row"
    let description = "A row of 12 brick dominoes toppled by a push."

    let run (s: PhysicsClient.Session.Session) =
        resetSimulation s

        // Camera: side view along the row
        setCamera s (-2.0, 3.0, 6.0) (3.0, 0.5, 0.0) |> ignore

        // Batch-create 12 dominoes — pre-generate IDs for push reference
        let ids = [ for _ in 0..11 -> nextId "box" ]
        let cmds =
            [ for i in 0..11 do
                let x = float i * 0.5
                makeBoxCmd ids.[i] (x, 0.3, 0.0) (0.05, 0.3, 0.15) 1.0 ]
        batchAdd s cmds
        let firstId = ids.[0]

        printfn "  Placed 12 dominoes in a row"

        // Let them settle standing
        runFor s 1.0

        // Push the first domino
        printfn "  Toppling first domino..."
        push s firstId East 3.0 |> ignore
        runFor s 5.0

        // Pan camera to the end
        setCamera s (8.0, 2.0, 4.0) (5.0, 0.0, 0.0) |> ignore
        sleep 500
        listBodies s
