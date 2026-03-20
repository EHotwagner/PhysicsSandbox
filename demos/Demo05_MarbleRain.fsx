// Demo 05: Marble Rain
// Random spheres rain down from the sky using the random generator.

module Demo05 =
    let name = "Marble Rain"
    let description = "20 random spheres rain down from the sky."

    let run (s: PhysicsClient.Session.Session) =
        resetScene s

        // Camera: overhead angle
        setCamera s (8.0, 12.0, 8.0) (0.0, 0.0, 0.0) |> ignore

        // Generate 20 random spheres (seed for reproducibility)
        let ids = randomSpheres s 20 (Some 42) |> ok
        printfn "  Generated %d random spheres" ids.Length

        printfn "  Let it rain..."
        runFor s 5.0

        // Switch to ground-level camera to see the aftermath
        setCamera s (3.0, 1.0, 3.0) (0.0, 0.5, 0.0) |> ignore
        printfn "  Ground-level view of the aftermath"
        sleep 1000
        listBodies s
