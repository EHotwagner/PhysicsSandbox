// Demo 07: Spinning Tops
// Several bodies with torque applied — spinning in place.

module Demo07 =
    let name = "Spinning Tops"
    let description = "Beach balls and crates spinning with applied torques."

    let run (s: PhysicsClient.Session.Session) =
        resetScene s

        // Camera: top-down angled view
        setCamera s (0.0, 8.0, 6.0) (0.0, 0.5, 0.0) |> ignore

        // Place bodies in a circle
        let b1 = beachBall s (Some (-2.0, 0.25, 0.0)) None None |> ok
        let b2 = beachBall s (Some (2.0, 0.25, 0.0)) None None |> ok
        let b3 = crate s (Some (0.0, 0.55, -2.0)) None None |> ok
        let b4 = crate s (Some (0.0, 0.55, 2.0)) None None |> ok
        printfn "  Placed 4 bodies in a circle"

        runFor s 0.5

        // Apply different spin axes and magnitudes
        spin s b1 Up 50.0 |> ignore
        spin s b2 North 30.0 |> ignore
        spin s b3 Up 80.0 |> ignore
        spin s b4 East 40.0 |> ignore
        printfn "  Applied torques — spinning..."

        runFor s 4.0

        // Wireframe mode for visual effect
        wireframe s true |> ignore
        printfn "  Wireframe view"
        sleep 2000
        wireframe s false |> ignore
        listBodies s
