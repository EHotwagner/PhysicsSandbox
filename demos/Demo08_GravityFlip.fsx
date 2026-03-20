// Demo 08: Gravity Flip
// Objects settle under normal gravity, then gravity reverses!

module Demo08 =
    let name = "Gravity Flip"
    let description = "Objects settle, then gravity flips upward — chaos!"

    let run (s: PhysicsClient.Session.Session) =
        resetScene s

        // Camera: wide angle
        setCamera s (6.0, 4.0, 6.0) (0.0, 2.0, 0.0) |> ignore

        // Build a 3x3 grid of crates and scatter some spheres
        grid s 3 3 (Some (-2.0, 0.0, -2.0)) |> ok |> ignore
        for i in 0..4 do
            let x = float i * 0.8 - 1.6
            beachBall s (Some (x, 5.0 + float i, 0.3)) None None |> ignore
        printfn "  Grid of crates + 5 beach balls dropping"

        // Normal gravity — let things settle
        runFor s 3.0
        printfn "  Settled under normal gravity"

        // Camera: looking up to see them fly
        setCamera s (5.0, 1.0, 5.0) (0.0, 5.0, 0.0) |> ignore

        // FLIP GRAVITY!
        printfn "  GRAVITY REVERSED!"
        setGravity s (0.0, 15.0, 0.0) |> ignore
        runFor s 3.0

        // Sideways gravity
        printfn "  Sideways gravity..."
        setGravity s (10.0, 0.0, 0.0) |> ignore
        setCamera s (-8.0, 3.0, 0.0) (0.0, 2.0, 0.0) |> ignore
        runFor s 2.0

        // Restore
        setGravity s (0.0, -9.81, 0.0) |> ignore
        printfn "  Gravity restored"
        runFor s 2.0
        listBodies s
