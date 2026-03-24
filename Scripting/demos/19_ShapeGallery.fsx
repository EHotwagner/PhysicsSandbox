#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay
open PhysicsSandbox.Shared.Contracts

let run (s: Session) =
    resetSimulation s
    setNarration s "Shape Gallery — every physics shape type"
    smoothCamera s (8.0, 8.0, 15.0) (0.0, 2.0, 0.0) 1.5
    sleep 1700
    setDemoInfo s "Demo 19: Shape Gallery" "All shape types displayed side-by-side — the complete physics shape catalog."
    let h = 6.0
    batchAdd s [ makeSphereCmd (nextId "sphere") (-6.0, h, 0.0) 0.3 2.0
                 |> withColorAndMaterial (Some projectileColor) None ]
    batchAdd s [ makeBoxCmd (nextId "box") (-4.0, h, 0.0) (0.25, 0.25, 0.25) 3.0
                 |> withColorAndMaterial (Some targetColor) None ]
    batchAdd s [ makeCapsuleCmd (nextId "capsule") (-2.0, h, 0.0) 0.15 0.4 2.0
                 |> withColorAndMaterial (Some accentGreen) None ]
    batchAdd s [ makeCylinderCmd (nextId "cylinder") (0.0, h, 0.0) 0.2 0.5 3.0
                 |> withColorAndMaterial (Some accentOrange) None ]
    batchAdd s [ makeTriangleCmd (nextId "triangle") (2.0, h, 0.0) (-0.3, -0.3, -0.3) (0.3, -0.3, -0.3) (0.0, 0.3, 0.3) 1.0
                 |> withColorAndMaterial (Some kinematicColor) None ]
    let octaPts = [(0.4,0.0,0.0);(-0.4,0.0,0.0);(0.0,0.4,0.0);(0.0,-0.4,0.0);(0.0,0.0,0.4);(0.0,0.0,-0.4)]
    batchAdd s [ makeConvexHullCmd (nextId "hull") (4.0, h, 0.0) octaPts 2.5
                 |> withColorAndMaterial (Some accentPurple) None ]
    let meshTris = [
      ((-0.3, -0.2, -0.3), (0.3, -0.2, -0.3), (0.0, -0.2, 0.3))
      ((-0.3, -0.2, -0.3), (0.3, -0.2, -0.3), (0.0, 0.3, 0.0))
      ((0.3, -0.2, -0.3), (0.0, -0.2, 0.3), (0.0, 0.3, 0.0))
      ((-0.3, -0.2, -0.3), (0.0, -0.2, 0.3), (0.0, 0.3, 0.0)) ]
    batchAdd s [ makeMeshCmd (nextId "mesh") (6.0, h, 0.0) meshTris 2.0
                 |> withColorAndMaterial (Some accentYellow) None ]
    let cs1 = Shape()
    let csp1 = Sphere()
    csp1.Radius <- 0.15
    cs1.Sphere <- csp1
    let cs2 = Shape()
    let csp2 = Sphere()
    csp2.Radius <- 0.15
    cs2.Sphere <- csp2
    batchAdd s [ makeCompoundCmd (nextId "compound") (8.0, h, 0.0) [(cs1, (-0.3, 0.0, 0.0)); (cs2, (0.3, 0.0, 0.0))] 3.0
                 |> withColorAndMaterial (Some (makeColor 1.0 0.3 0.7 1.0)) None ]
    setNarration s "All shapes dropping — watch the variety"
    printfn "  All shape types dropping from %.0fm" h
    runFor s 4.0
    setNarration s "Close-up — shapes at rest on the ground"
    smoothCamera s (4.0, 1.5, 8.0) (1.0, 0.3, 0.0) 2.0
    sleep 2200
    runFor s 2.0
    setNarration s "Panning across the gallery"
    smoothCamera s (-4.0, 2.0, 8.0) (-2.0, 0.3, 0.0) 2.0
    sleep 2200
    clearNarration s
    listBodies s

runStandalone "Demo 19: Shape Gallery" run
