// Demo 02: Bouncing Marbles
// Several marbles dropped from different heights — watch them settle.

module Demo02 =
    let name = "Bouncing Marbles"
    let description = "Five marbles dropped from different heights."

    let run (s: PhysicsClient.Session.Session) =
        resetScene s

        // Camera: elevated overview
        setCamera s (4.0, 5.0, 4.0) (0.0, 0.5, 0.0) |> ignore

        // Drop marbles at various heights and offsets
        for i in 0..4 do
            let x = float i * 0.3 - 0.6
            let y = 3.0 + float i * 2.0
            marble s (Some (x, y, 0.0)) None None |> ignore

        printfn "  Dropping 5 marbles from 3m to 11m..."
        runFor s 4.0
        listBodies s
