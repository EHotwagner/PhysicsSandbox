// Demo 04: Bowling Alley
// A bowling ball launched at a pyramid of bricks.

module Demo04 =
    let name = "Bowling Alley"
    let description = "Launch a bowling ball at a pyramid of bricks."

    let run (s: PhysicsClient.Session.Session) =
        resetSimulation s

        // Camera: behind the ball looking at pins
        setCamera s (-5.0, 3.0, 3.0) (3.0, 1.0, 0.0) |> ignore

        // Build a 4-layer pyramid of bricks at the far end
        pyramid s 4 (Some (5.0, 0.0, 0.0)) |> ok |> ignore
        printfn "  Built pyramid (4 layers)"

        // Place bowling ball at the near end
        let ball = bowlingBall s (Some (-3.0, 0.15, 0.0)) None None |> ok
        printfn "  Bowling ball ready at (-3, 0, 0)"

        // Let scene settle
        runFor s 1.0

        // Launch the ball at the pyramid!
        printfn "  STRIKE! Launching ball..."
        launch s ball (5.0, 0.5, 0.0) 25.0 |> ignore
        runFor s 4.0
        listBodies s
