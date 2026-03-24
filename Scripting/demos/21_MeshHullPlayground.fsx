#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay
open PhysicsSandbox.Shared.Contracts

let run (s: Session) =
    resetSimulation s
    setCamera s (8.0, 8.0, 12.0) (0.0, 2.0, 0.0) |> ignore
    setDemoInfo s "Demo 21: Mesh & Hull Playground" "Convex hulls and triangle meshes of varied complexity tumbling through obstacles."
    for i in 0 .. 2 do
      let x = float i * 3.0 - 3.0
      batchAdd s [ makeBoxCmd (nextId "obstacle") (x, 0.5, 0.0) (0.3, 0.5, 2.0) 0.0
                   |> withColorAndMaterial (Some structureColor) None ]
    let h = 8.0
    for i in 0 .. 4 do
      let x = float i * 2.0 - 4.0
      let sv = 0.2 + float i * 0.08
      let tetraPts = [(sv, 0.0, 0.0); (-sv, 0.0, 0.0); (0.0, sv * 1.5, 0.0); (0.0, sv * 0.5, sv)]
      batchAdd s [ makeConvexHullCmd (nextId "tetra") (x, h, float i * 0.2 - 0.4) tetraPts (1.0 + float i * 0.5)
                   |> withColorAndMaterial (Some (makeColor 0.3 (0.5 + float i * 0.1) 1.0 1.0)) None ]
    for i in 0 .. 3 do
      let x = float i * 2.5 - 3.5
      let r = 0.25 + float i * 0.05
      let octaPts = [(r,0.0,0.0);(-r,0.0,0.0);(0.0,r,0.0);(0.0,-r,0.0);(0.0,0.0,r);(0.0,0.0,-r)]
      batchAdd s [ makeConvexHullCmd (nextId "octa") (x, h + 2.0, 0.0) octaPts (2.0 + float i * 0.8)
                   |> withColorAndMaterial (Some accentPurple) None ]
    for i in 0 .. 3 do
      let x = float i * 2.5 - 3.0
      let sv = 0.25 + float i * 0.05
      let meshTris = [
        ((-sv, 0.0, -sv), (sv, 0.0, -sv), (0.0, 0.0, sv))
        ((-sv, 0.0, -sv), (sv, 0.0, -sv), (0.0, sv * 1.5, 0.0))
        ((sv, 0.0, -sv), (0.0, 0.0, sv), (0.0, sv * 1.5, 0.0))
        ((-sv, 0.0, -sv), (0.0, 0.0, sv), (0.0, sv * 1.5, 0.0)) ]
      batchAdd s [ makeMeshCmd (nextId "mesh") (x, h + 4.0, float i * 0.3) meshTris (1.5 + float i * 0.4)
                   |> withColorAndMaterial (Some kinematicColor) None ]
    for i in 0 .. 5 do
      let x = float i * 1.5 - 3.5
      batchAdd s [ makeTriangleCmd (nextId "tri") (x, h + 6.0, float i * 0.2 - 0.5) (-0.25, -0.2, -0.2) (0.25, -0.2, -0.2) (0.0, 0.25, 0.2) 0.8
                   |> withColorAndMaterial (Some (makeColor 1.0 (float i * 0.15) 0.3 1.0)) None ]
    printfn "  Dropping tetrahedra, octahedra, mesh pyramids, and triangles onto obstacles..."
    runFor s 5.0
    setCamera s (4.0, 2.0, 7.0) (0.0, 1.0, 0.0) |> ignore
    runFor s 2.0
    listBodies s

runStandalone "Demo 21: Mesh & Hull Playground" run
