// Demo 09: Billiards
// A grid of balls on a "table", cue ball launched into the formation.
// Usage: dotnet fsi Scripting/demos/Demo09_Billiards.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Steering
open PhysicsClient.StateDisplay

module Demo09 =
    let name = "Billiards"
    let description = "Cue ball breaks a triangle formation on a table."

    let run (s: Session) =
        resetSimulation s

        // Reduce gravity for billiards-like sliding (low friction sim)
        setGravity s (0.0, -9.81, 0.0) |> ignore

        setDemoInfo s "Demo 09: Billiards" "Billiard ball collision mechanics — break shot and rebounds."
        setNarration s "Top-down view — setting up the triangle formation"
        smoothCamera s (0.0, 6.0, 0.1) (0.0, 0.0, 0.0) 1.5
        sleep 1700

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

        // Pause to admire the triangle formation
        setNarration s "15 balls in triangle — cue ball ready"
        printfn "  Admiring the formation..."
        sleep 1500

        runFor s 0.5

        // BREAK!
        setNarration s "BREAK — cue ball launched!"
        smoothCamera s (-1.0, 4.0, 2.0) (0.5, 0.0, 0.0) 1.0
        sleep 1200
        printfn "  BREAK!"
        launch s cueId (1.5, 0.0, 0.0) 7.5 |> ignore
        runFor s 2.0

        // Zoom out to see the full spread
        setNarration s "Zooming out — balls spreading across the table"
        smoothCamera s (0.0, 12.0, 0.1) (0.0, 0.0, 0.0) 1.5
        sleep 1700
        runFor s 2.0

        setNarration s "Final positions — break complete"
        smoothCamera s (0.0, 8.0, 3.0) (0.0, 0.0, 0.0) 1.5
        sleep 1700
        clearNarration s
        listBodies s

runStandalone Demo09.name Demo09.run
