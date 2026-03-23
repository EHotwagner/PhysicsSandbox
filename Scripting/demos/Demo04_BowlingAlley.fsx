// Demo 04: Bowling Alley — Launch a bowling ball at a pyramid of bricks.
// Usage: dotnet fsi Scripting/demos/Demo04_BowlingAlley.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets
open PhysicsClient.Generators
open PhysicsClient.Steering
open PhysicsClient.StateDisplay

let name = "Bowling Alley"

let run s =
    resetSimulation s
    // Start focused on the pyramid
    setCamera s (0.0, 4.0, 12.0) (0.0, 0.5, 5.0) |> ignore
    // 6x5 brick wall — small bricks (10cm cubes) so the boulder clearly smashes through
    let brickIds =
        [ for row in 0 .. 4 do
            for col in 0 .. 5 do
                let x = float (col - 2) * 0.22 - 0.11
                let z = 5.0
                let y = 0.11 + float row * 0.22
                let id = nextId "box"
                let colors = [| accentYellow; accentOrange; projectileColor; accentPurple; accentGreen |]
                batchAdd s [ makeBoxCmd id (x, y, z) (0.1, 0.1, 0.1) 0.5
                             |> withColorAndMaterial (Some colors.[row % colors.Length]) None ]
                id ]
    printfn "  Built wall of %d bricks" brickIds.Length
    // Big wrecking ball (r=0.4, 10kg) — big enough to smash bricks, light enough to fly
    let ballId = nextId "sphere"
    batchAdd s [ makeSphereCmd ballId (0.0, 0.5, -4.0) 0.4 10.0
                 |> withColorAndMaterial (Some targetColor) None ]
    printfn "  Wrecking ball ready"
    runFor s 1.0
    printfn "  Admiring the wall..."
    sleep 1500
    printfn "  SMASH!"
    batchAdd s [ makeImpulseCmd ballId (0.0, 0.0, 100.0) ]
    runFor s 2.5
    printfn "  Debris settling..."
    runFor s 3.0
    listBodies s

runStandalone name run
