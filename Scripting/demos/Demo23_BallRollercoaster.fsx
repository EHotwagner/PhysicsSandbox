#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay
open PhysicsSandbox.Shared.Contracts

/// Height function for the rollercoaster terrain.
/// x: lateral position, z: forward position along the track.
/// Returns y height. Track runs from z=-15 to z=15.
let terrainHeight (x: float) (z: float) =
    let pi = System.Math.PI
    // Normalize z to [0..1] range
    let t = (z + 15.0) / 30.0 |> max 0.0 |> min 1.0
    // Base elevation profile along the track
    let baseY =
        if t < 0.1 then
            8.0 // launch platform
        elif t < 0.25 then
            8.0 - (t - 0.1) / 0.15 * 6.0 // steep drop
        elif t < 0.45 then
            2.0 + 3.0 * sin((t - 0.25) / 0.20 * pi) // hill
        elif t < 0.65 then
            2.0 - (t - 0.45) * 3.0 // descent
        elif t < 0.85 then
            0.8 + 2.0 * sin((t - 0.65) / 0.20 * pi) // second hill
        else
            0.8 - (t - 0.85) / 0.15 * 0.3 // final run-out
    // Channel shape: raised edges to keep balls centered
    let edgeLift = 0.3 * (x * x) / 4.0 // parabolic channel walls
    // Banking in the middle section
    let bank =
        if t > 0.35 && t < 0.55 then
            0.3 * sin((t - 0.35) / 0.20 * pi) * x / 3.0
        else 0.0
    baseY + edgeLift + bank

/// Generate the track as a heightmap grid of triangles.
let generateTrack () =
    let xMin, xMax = -3.0, 3.0
    let zMin, zMax = -15.0, 15.0
    let xSteps = 4   // 4 columns (each ~1.5m wide)
    let zSteps = 15  // 15 rows (each 2m long)
    let dx = (xMax - xMin) / float xSteps
    let dz = (zMax - zMin) / float zSteps

    let mutable triangles = []
    for zi in 0 .. zSteps - 1 do
        for xi in 0 .. xSteps - 1 do
            let x0 = xMin + float xi * dx
            let x1 = x0 + dx
            let z0 = zMin + float zi * dz
            let z1 = z0 + dz
            let p00 = (x0, terrainHeight x0 z0, z0)
            let p10 = (x1, terrainHeight x1 z0, z0)
            let p01 = (x0, terrainHeight x0 z1, z1)
            let p11 = (x1, terrainHeight x1 z1, z1)
            // Two triangles per quad
            triangles <- (p00, p10, p01) :: triangles
            triangles <- (p10, p11, p01) :: triangles
    triangles |> List.rev

let run (s: Session) =
    resetSimulation s
    setDemoInfo s "Demo 23: Ball Rollercoaster" "Balls roll down a mesh terrain with drops, hills, and banked curves."

    setNarration s "Building rollercoaster terrain..."
    smoothCamera s (8.0, 14.0, -10.0) (0.0, 4.0, 0.0) 1.5
    sleep 1700

    // Build terrain
    let trackTris = generateTrack ()
    printfn "  Track generated: %d triangles" trackTris.Length
    batchAdd s [ makeMeshCmd (nextId "track") (0.0, 0.0, 0.0) trackTris 0.0
                 |> withMotionType BodyMotionType.Static
                 |> withColorAndMaterial (Some accentYellow) (Some slipperyMaterial) ]

    sleep 500

    // Spawn balls on the launch platform (z=-15, x=0, y=8 + offset)
    setNarration s "Releasing balls at the top!"
    smoothCamera s (4.0, 10.0, -16.0) (0.0, 8.0, -13.0) 1.5
    sleep 1700

    let ballColors = [
        projectileColor; accentGreen; targetColor;
        accentOrange; accentPurple; kinematicColor
    ]
    let ballCmds =
        [ for i in 0 .. 5 ->
            makeSphereCmd (nextId "ball") (0.0, 9.0, -14.5 + float i * 0.6) 0.3 2.0
            |> withColorAndMaterial (Some ballColors.[i]) None ]
    batchAdd s ballCmds

    // Watch the drop
    setNarration s "Steep drop — balls accelerating!"
    play s |> ignore
    smoothCamera s (5.0, 7.0, -8.0) (0.0, 3.0, -3.0) 2.0
    sleep 3500

    // Hill
    setNarration s "Over the hill!"
    smoothCamera s (-5.0, 7.0, 0.0) (0.0, 3.0, 3.0) 2.0
    sleep 3000

    // Banked descent
    setNarration s "Banked descent"
    smoothCamera s (-6.0, 5.0, 6.0) (0.0, 1.5, 9.0) 2.0
    sleep 3000

    // Second hill
    setNarration s "Second hill and run-out"
    smoothCamera s (5.0, 5.0, 10.0) (0.0, 1.5, 14.0) 2.0
    sleep 3000

    // Wide overview
    setNarration s "Full terrain overview"
    smoothCamera s (12.0, 14.0, 0.0) (0.0, 3.0, 0.0) 2.0
    sleep 2500

    pause s |> ignore
    clearNarration s
    printfn "  Rollercoaster demo complete!"
    listBodies s

runStandalone "Demo 23: Ball Rollercoaster" run
