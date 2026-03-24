// 18_KinematicSweep.fsx — Kinematic bulldozer plows through dynamic bodies
// Usage: dotnet fsi Scripting/demos/18_KinematicSweep.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay
open PhysicsSandbox.Shared.Contracts

let run (s: Session) =
    // 1. Reset and set camera
    resetSimulation s
    setCamera s (10.0, 6.0, 10.0) (0.0, 1.0, 0.0) |> ignore
    setDemoInfo s "Demo 18: Kinematic Sweep" "Kinematic bulldozer plows through dynamic spheres."
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

    // 5. Drive spinning bulldozer forward — linear velocity + angular spin
    let speed = 2.0 // m/s forward
    let spin = 5.0  // rad/s around Y axis
    printfn "  %d dynamic spheres + 1 spinning kinematic bulldozer" sphereCmds.Length
    printfn "  Bulldozer advancing at %.1f m/s with %.1f rad/s Y-spin..." speed spin
    setBodyPose s "bulldozer" (-4.0, 0.5, 0.0) None (Some (speed, 0.0, 0.0)) (Some (0.0, spin, 0.0)) |> ok
    play s |> ignore
    for step in 0 .. 19 do
        sleep 200
        if step % 5 = 0 then
            printfn "  Step %d/20 — t=%.1fs" (step + 1) (float (step + 1) * 0.2)
    pause s |> ignore

    // 6. Final camera sweep and status
    printfn "  Sweep complete! Final overview..."
    setCamera s (0.0, 10.0, 12.0) (0.0, 0.5, 0.0) |> ignore
    sleep 1000
    setCamera s (8.0, 4.0, 8.0) (0.0, 0.5, 0.0) |> ignore
    sleep 1000
    listBodies s

runStandalone "Kinematic Sweep" run
