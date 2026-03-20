// Demo 10: Chaos Scene
// Everything combined: presets, generators, steering, gravity changes,
// camera animation — the full sandbox experience.

module Demo10 =
    let name = "Chaos Scene"
    let description = "The full sandbox: presets, generators, steering, gravity, camera sweeps."

    let run (s: PhysicsClient.Session.Session) =
        resetScene s

        // Act 1: Build the stage
        setCamera s (12.0, 8.0, 12.0) (0.0, 2.0, 0.0) |> ignore
        printfn "  Act 1: Building the stage..."

        // Pyramid on the left
        pyramid s 5 (Some (-4.0, 0.0, 0.0)) |> ok |> ignore
        // Stack on the right
        stack s 6 (Some (4.0, 0.0, 0.0)) |> ok |> ignore
        // Row of spheres in the middle
        row s 8 (Some (-3.0, 0.0, 3.0)) |> ok |> ignore

        runFor s 1.5
        printfn "  Stage built: pyramid + stack + row"

        // Act 2: Bombardment from above
        setCamera s (0.0, 15.0, 10.0) (0.0, 2.0, 0.0) |> ignore
        printfn "  Act 2: Bombardment!"

        let projectiles = randomSpheres s 10 (Some 99) |> ok
        for id in projectiles do
            pushVec s id (0.0, -20.0, 0.0) |> ignore

        runFor s 3.0

        // Act 3: Boulder attack
        setCamera s (-10.0, 3.0, 0.0) (0.0, 2.0, 0.0) |> ignore
        printfn "  Act 3: Boulder attack on pyramid!"

        let rock = boulder s (Some (-8.0, 1.0, 0.0)) None None |> ok
        launch s rock (-4.0, 2.0, 0.0) 30.0 |> ignore
        runFor s 3.0

        // Act 4: Gravity chaos
        printfn "  Act 4: Gravity chaos!"
        setCamera s (8.0, 2.0, 8.0) (0.0, 3.0, 0.0) |> ignore
        setGravity s (0.0, 8.0, 0.0) |> ignore
        runFor s 2.0

        setGravity s (5.0, 0.0, 5.0) |> ignore
        setCamera s (-6.0, 4.0, -6.0) (2.0, 2.0, 2.0) |> ignore
        runFor s 2.0

        // Act 5: Spin everything remaining
        printfn "  Act 5: Spin everything!"
        setGravity s (0.0, -9.81, 0.0) |> ignore
        setCamera s (10.0, 6.0, 10.0) (0.0, 1.0, 0.0) |> ignore

        // Wireframe for dramatic effect
        wireframe s true |> ignore
        runFor s 2.0
        wireframe s false |> ignore

        // Final camera sweep
        printfn "  Final: Camera sweep"
        for angle in 0..8 do
            let a = float angle * 0.7
            let cx = 10.0 * cos a
            let cz = 10.0 * sin a
            setCamera s (cx, 5.0, cz) (0.0, 1.0, 0.0) |> ignore
            sleep 400

        printfn "  Chaos complete!"
        status s
