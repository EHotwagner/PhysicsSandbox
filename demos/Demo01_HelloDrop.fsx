// Demo 01: Hello Drop
// A single bowling ball falls from height onto the ground.
// The simplest possible physics demo.

module Demo01 =
    let name = "Hello Drop"
    let description = "A single bowling ball falls from height onto the ground."

    let run (s: PhysicsClient.Session.Session) =
        resetSimulation s

        // Camera: side view
        setCamera s (5.0, 3.0, 5.0) (0.0, 1.0, 0.0) |> ignore

        // Drop a bowling ball from 10 meters
        bowlingBall s (Some (0.0, 10.0, 0.0)) None None |> ignore

        printfn "  Dropping bowling ball from 10m..."
        runFor s 3.0
        listBodies s
