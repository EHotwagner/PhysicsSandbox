// Demo 09: Billiards
// A grid of balls on a "table", cue ball launched into the formation.

module Demo09 =
    let name = "Billiards"
    let description = "Cue ball breaks a triangle formation on a table."

    let run (s: PhysicsClient.Session.Session) =
        resetScene s

        // Reduce gravity for billiards-like sliding (low friction sim)
        setGravity s (0.0, -9.81, 0.0) |> ignore

        // Camera: overhead billiards view
        setCamera s (0.0, 10.0, 0.1) (0.0, 0.0, 0.0) |> ignore

        // Arrange balls in triangle formation (like pool rack)
        let r = 0.1
        let spacing = 0.22
        let mutable ids = []
        for row in 0..4 do
            for col in 0..row do
                let x = float row * spacing * 0.866 + 1.0
                let z = (float col - float row / 2.0) * spacing
                let id = addSphere s (x, r, z) r 0.17 None |> ok
                ids <- id :: ids
        printfn "  Placed %d balls in triangle" ids.Length

        // Cue ball
        let cue = addSphere s (-2.0, r, 0.0) (r * 1.1) 0.17 (Some "cue") |> ok
        printfn "  Cue ball placed"

        // Camera: dramatic low angle
        setCamera s (-3.0, 1.5, 2.0) (1.0, 0.0, 0.0) |> ignore

        runFor s 0.5

        // BREAK!
        printfn "  BREAK!"
        launch s cue (1.5, 0.0, 0.0) 15.0 |> ignore
        runFor s 4.0

        // Top-down aftermath
        setCamera s (0.0, 8.0, 0.1) (0.0, 0.0, 0.0) |> ignore
        sleep 1000
        listBodies s
