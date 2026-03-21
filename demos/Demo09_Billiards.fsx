// Demo 09: Billiards
// A grid of balls on a "table", cue ball launched into the formation.

module Demo09 =
    let name = "Billiards"
    let description = "Cue ball breaks a triangle formation on a table."

    let run (s: PhysicsClient.Session.Session) =
        resetSimulation s

        // Reduce gravity for billiards-like sliding (low friction sim)
        setGravity s (0.0, -9.81, 0.0) |> ignore

        // Camera: overhead billiards view
        setCamera s (0.0, 10.0, 0.1) (0.0, 0.0, 0.0) |> ignore

        // Batch-create 15 balls in triangle + 1 cue ball
        let r = 0.1
        let spacing = 0.22
        let cueId = "cue"
        let cmds = [
            for row in 0..4 do
                for col in 0..row do
                    let x = float row * spacing * 0.866 + 1.0
                    let z = (float col - float row / 2.0) * spacing
                    makeSphereCmd (nextId "sphere") (x, r, z) r 0.17
            makeSphereCmd cueId (-2.0, r, 0.0) (r * 1.1) 0.17 ]
        batchAdd s cmds
        printfn "  Placed 15 balls in triangle + cue ball"

        // Camera: dramatic low angle
        setCamera s (-3.0, 1.5, 2.0) (1.0, 0.0, 0.0) |> ignore

        runFor s 0.5

        // BREAK!
        printfn "  BREAK!"
        launch s cueId (1.5, 0.0, 0.0) 15.0 |> ignore
        runFor s 4.0

        // Top-down aftermath
        setCamera s (0.0, 8.0, 0.1) (0.0, 0.0, 0.0) |> ignore
        sleep 1000
        listBodies s
