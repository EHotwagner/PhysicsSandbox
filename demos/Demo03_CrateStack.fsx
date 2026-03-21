// Demo 03: Crate Stack
// A tower of crates stacked using the scene builder.

module Demo03 =
    let name = "Crate Stack"
    let description = "A tower of 8 crates built with the stack generator."

    let run (s: PhysicsClient.Session.Session) =
        resetSimulation s

        // Camera: front view, slightly elevated
        setCamera s (6.0, 5.0, 0.0) (0.0, 4.0, 0.0) |> ignore

        // Build a stack of 8 crates
        let ids = stack s 8 (Some (0.0, 0.0, 0.0)) |> ok
        printfn "  Built stack of %d crates" ids.Length

        // Let it settle
        runFor s 2.0

        // Now push the top crate sideways
        let topId = ids |> List.last
        printfn "  Pushing top crate east..."
        push s topId East 15.0 |> ignore
        runFor s 3.0
        listBodies s
