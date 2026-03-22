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
    setCamera s (0.0, 2.0, 8.0) (0.0, 0.5, 5.0) |> ignore
    // 6x5 brick wall — small bricks (10cm cubes) so the boulder clearly smashes through
    let brickIds =
        [ for row in 0 .. 4 do
            for col in 0 .. 5 do
                let x = float (col - 2) * 0.22 - 0.11
                let z = 5.0
                let y = 0.11 + float row * 0.22
                let id = nextId "box"
                batchAdd s [ makeBoxCmd id (x, y, z) (0.1, 0.1, 0.1) 0.5 ]
                id ]
    printfn "  Built wall of %d bricks" brickIds.Length
    // Big wrecking ball (r=0.4, 10kg) — big enough to smash bricks, light enough to fly
    let ballId = nextId "sphere"
    batchAdd s [ makeSphereCmd ballId (0.0, 0.5, -4.0) 0.4 10.0 ]
    printfn "  Wrecking ball ready"
    runFor s 1.0
    printfn "  Admiring the wall..."
    sleep 1500
    setCamera s (0.0, 2.5, -6.0) (0.0, 0.5, 5.0) |> ignore
    printfn "  SMASH!"
    batchAdd s [ makeImpulseCmd ballId (0.0, 0.0, 100.0) ]
    runFor s 2.5
    // Side view of the debris
    setCamera s (4.0, 1.5, 5.0) (0.0, 0.3, 5.0) |> ignore
    printfn "  Debris view"
    runFor s 3.0
    listBodies s

runStandalone name run
