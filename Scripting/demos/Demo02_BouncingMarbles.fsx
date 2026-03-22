// Demo 02: Bouncing Marbles
// Several marbles dropped from different heights — watch them settle.

module Demo02 =
    let name = "Bouncing Marbles"
    let description = "Five marbles dropped from different heights."

    let run (s: PhysicsClient.Session.Session) =
        resetSimulation s

        // Camera: elevated overview
        setCamera s (4.0, 5.0, 4.0) (0.0, 0.5, 0.0) |> ignore

        // Batch-create 5 marbles at various heights (r=0.01, m=0.005)
        let cmds =
            [ for i in 0..4 do
                let x = float i * 0.3 - 0.6
                let y = 3.0 + float i * 2.0
                makeSphereCmd (nextId "sphere") (x, y, 0.0) 0.01 0.005 ]
        batchAdd s cmds

        printfn "  Dropping 5 marbles from 3m to 11m..."
        runFor s 4.0
        listBodies s
