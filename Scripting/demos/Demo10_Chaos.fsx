// Demo 10: Chaos Scene
// Everything combined: presets, generators, steering, gravity changes,
// camera animation — the full sandbox experience.
// Usage: dotnet fsi Scripting/demos/Demo10_Chaos.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.Presets
open PhysicsClient.Generators
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Steering
open PhysicsClient.StateDisplay

module Demo10 =
    let name = "Chaos Scene"
    let description = "The full sandbox: presets, generators, steering, gravity, camera sweeps."

    let run (s: Session) =
        resetSimulation s

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

        // Dramatic pause — let the audience take in the stage
        sleep 800

        // Act 2: Barrage from the side at pyramid and stack
        setCamera s (0.0, 6.0, 14.0) (0.0, 2.0, 0.0) |> ignore
        printfn "  Act 2: Side barrage!"

        // Spawn 10 spheres to the right, launch at the pyramid (-4, ~2, 0)
        for i in 0 .. 4 do
            let y = 1.0 + float i * 0.8
            let sid = nextId "proj"
            addSphere s (10.0, y, 0.0) 0.3 3.0 (Some sid) None None None None None |> ignore
            launch s sid (-4.0, y, 0.0) 200.0 |> ignore
        sleep 500
        // Spawn 5 more, launch at the stack (4, ~2, 0)
        for i in 0 .. 4 do
            let y = 1.0 + float i * 0.8
            let sid = nextId "proj"
            addSphere s (-10.0, y, 0.0) 0.3 3.0 (Some sid) None None None None None |> ignore
            launch s sid (4.0, y, 0.0) 200.0 |> ignore
        runFor s 3.0

        // Act 3: Boulder attack on whatever's left
        setCamera s (-10.0, 3.0, 6.0) (0.0, 2.0, 0.0) |> ignore
        printfn "  Act 3: Boulder attack!"

        let rock = boulder s (Some (-8.0, 1.0, 0.0)) None None |> ok
        launch s rock (0.0, 2.0, 0.0) 30.0 |> ignore
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

        // Final camera sweep (tighter pacing)
        printfn "  Final: Camera sweep"
        for angle in 0..6 do
            let a = float angle * 0.9
            let cx = 10.0 * cos a
            let cz = 10.0 * sin a
            setCamera s (cx, 5.0, cz) (0.0, 1.0, 0.0) |> ignore
            sleep 300

        printfn "  Chaos complete!"
        status s

runStandalone Demo10.name Demo10.run
