// 18_KinematicSweep.fsx — Kinematic bulldozer plows through dynamic bodies
// Usage: dotnet fsi Scripting/demos/18_KinematicSweep.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsSandbox.Shared.Contracts

let run (s: Session) =
    // 1. Reset and set camera
    resetSimulation s
    setCamera s (10.0, 6.0, 10.0) (0.0, 1.0, 0.0) |> ignore
    printfn "  Setting up kinematic sweep demo..."

    // 2. Place 30 small dynamic spheres in a 6x5 grid
    let sphereCmds =
        [ for col in 0 .. 5 do
            for row in 0 .. 4 do
                let x = float col * 0.8 - 2.0
                let z = float row * 0.8 - 1.6
                let id = nextId "sphere"
                makeSphereCmd id (x, 0.3, z) 0.15 0.5
                |> withColorAndMaterial (Some accentYellow) None ]
    batchAdd s sphereCmds
    printfn "  Placed %d dynamic spheres in 6x5 grid" sphereCmds.Length

    // 3. Create kinematic bulldozer box at (-4, 0.5, 0)
    let boxShape = Shape()
    let b = Box()
    b.HalfExtents <- toVec3 (0.5, 0.5, 0.5)
    boxShape.Box <- b

    let bulldozerCmd =
        makeKinematicCmd "bulldozer" (-4.0, 0.5, 0.0) boxShape
        |> withColorAndMaterial (Some kinematicColor) None
    batchAdd s [ bulldozerCmd ]
    printfn "  Created kinematic bulldozer at (-4, 0.5, 0)"

    // 4. Settle for 1 second
    printfn "  Settling..."
    runFor s 1.0

    // 5. Animate bulldozer through 20 steps
    printfn "  Starting bulldozer sweep..."
    for step in 0 .. 19 do
        let x = -4.0 + float step * 0.4
        setPose s "bulldozer" (x, 0.5, 0.0)
        play s |> ignore
        sleep 200
        pause s |> ignore
        if step % 5 = 4 then
            printfn "  Step %d/20 — bulldozer at x=%.1f" (step + 1) x

    // 6. Final camera sweep and status
    printfn "  Sweep complete! Final overview..."
    setCamera s (0.0, 10.0, 12.0) (0.0, 0.5, 0.0) |> ignore
    sleep 1000
    setCamera s (8.0, 4.0, 8.0) (0.0, 0.5, 0.0) |> ignore
    sleep 1000
    listBodies s

runStandalone "Kinematic Sweep" run
