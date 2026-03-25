#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay
open PhysicsSandbox.Shared.Contracts

/// Halfpipe height function: U-shaped cross-section.
/// x: lateral (-4 to 4), z: along pipe (-8 to 8)
/// Returns y = bowl-shaped height
let pipeHeight (x: float) (z: float) =
    let radius = 3.5
    // U-shape: y = radius - sqrt(radius^2 - x^2) for |x| < radius
    let ax = abs x |> min radius
    let baseY = radius - sqrt(radius * radius - ax * ax)
    // End caps: raise the floor at z extremes to form a bowl
    let zEdge = (abs z - 5.0) |> max 0.0  // starts rising at |z|>5
    let capLift = zEdge * zEdge * 0.15
    baseY + capLift

/// Generate the halfpipe as a heightmap grid.
let generateHalfpipe () =
    let xMin, xMax = -4.0, 4.0
    let zMin, zMax = -8.0, 8.0
    let xSteps = 6
    let zSteps = 8
    let dx = (xMax - xMin) / float xSteps
    let dz = (zMax - zMin) / float zSteps

    let mutable triangles = []
    for zi in 0 .. zSteps - 1 do
        for xi in 0 .. xSteps - 1 do
            let x0 = xMin + float xi * dx
            let x1 = x0 + dx
            let z0 = zMin + float zi * dz
            let z1 = z0 + dz
            let p00 = (x0, pipeHeight x0 z0, z0)
            let p10 = (x1, pipeHeight x1 z0, z0)
            let p01 = (x0, pipeHeight x0 z1, z1)
            let p11 = (x1, pipeHeight x1 z1, z1)
            triangles <- (p00, p10, p01) :: triangles
            triangles <- (p10, p11, p01) :: triangles
    triangles |> List.rev

let halfpipeMaterial = makeMaterialProperties 0.3 4.0 30.0 0.8

let run (s: Session) =
    resetSimulation s
    setDemoInfo s "Demo 24: Halfpipe Arena" "Objects oscillate in a halfpipe bowl built from mesh triangles."

    // Opening: elevated view looking down into the bowl
    setNarration s "Building halfpipe arena..."
    smoothCamera s (0.0, 12.0, 14.0) (0.0, 1.0, 0.0) 1.5
    sleep 1700

    let pipeTris = generateHalfpipe ()
    printfn "  Halfpipe generated: %d triangles" pipeTris.Length
    batchAdd s [ makeMeshCmd (nextId "halfpipe") (0.0, 0.0, 0.0) pipeTris 0.0
                 |> withMotionType BodyMotionType.Static
                 |> withColorAndMaterial (Some accentYellow) (Some halfpipeMaterial) ]

    sleep 500

    // Camera from end of pipe, looking along interior
    setNarration s "Dropping balls into the halfpipe!"
    smoothCamera s (0.0, 6.0, -14.0) (0.0, 1.0, 0.0) 1.5
    sleep 1700

    // Drop balls inside the pipe (x near center, above the lip)
    let ballColors = [
        projectileColor; accentGreen; targetColor;
        accentOrange; accentPurple; kinematicColor
    ]
    let ballCmds =
        [ for i in 0 .. 5 ->
            let x = (float i - 2.5) * 0.5  // x from -1.25 to 1.25 (inside the U)
            let z = (float i - 2.5) * 1.5  // z from -3.75 to 3.75
            makeSphereCmd (nextId "ball") (x, 5.0, z) 0.35 2.5
            |> withColorAndMaterial (Some ballColors.[i]) None ]
    batchAdd s ballCmds

    // Drop capsules
    let capCmds =
        [ for i in 0 .. 1 ->
            makeCapsuleCmd (nextId "capsule") (0.0, 6.0, float i * 3.0 - 1.5) 0.25 0.7 3.0
            |> withColorAndMaterial (Some accentPurple) None ]
    batchAdd s capCmds

    // Watch the drop from above
    setNarration s "Objects falling into the bowl!"
    play s |> ignore
    smoothCamera s (0.0, 10.0, 12.0) (0.0, 1.0, 0.0) 2.0
    sleep 4000

    // Side view — see oscillation profile
    setNarration s "Oscillation — rolling back and forth"
    smoothCamera s (10.0, 5.0, 0.0) (0.0, 1.5, 0.0) 2.0
    sleep 4000

    // End-on view
    setNarration s "Looking down the halfpipe"
    smoothCamera s (0.0, 5.0, -14.0) (0.0, 1.0, 0.0) 2.0
    sleep 3500

    // Above close-up
    setNarration s "Objects settling at the bottom"
    smoothCamera s (0.0, 8.0, 5.0) (0.0, 0.5, 0.0) 2.0
    sleep 3500

    // Second wave
    setNarration s "Second wave incoming!"
    let wave2 =
        [ for i in 0 .. 2 ->
            makeSphereCmd (nextId "ball") (float i - 1.0, 6.0, float i * 2.0 - 2.0) 0.4 3.0
            |> withColorAndMaterial (Some accentGreen) None ]
    batchAdd s wave2
    sleep 3000

    // Final wide shot
    setNarration s "Wide view — halfpipe arena"
    smoothCamera s (10.0, 10.0, 10.0) (0.0, 2.0, 0.0) 2.0
    sleep 2500

    pause s |> ignore
    clearNarration s
    printfn "  Halfpipe arena demo complete!"
    listBodies s

runStandalone "Demo 24: Halfpipe Arena" run
