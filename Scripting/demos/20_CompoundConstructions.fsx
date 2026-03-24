#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.StateDisplay
open PhysicsSandbox.Shared.Contracts

let run (s: Session) =
    resetSimulation s
    setCamera s (6.0, 8.0, 10.0) (0.0, 2.0, 0.0) |> ignore
    setDemoInfo s "Demo 20: Compound Constructions" "Complex compound shapes — L-shapes, T-shapes, dumbbells — colliding and stacking."
    let h = 8.0
    for i in 0 .. 4 do
      let x = float i * 2.0 - 4.0
      let bx1 = Shape()
      let box1 = Box()
      box1.HalfExtents <- toVec3 (0.4, 0.1, 0.1)
      bx1.Box <- box1
      let bx2 = Shape()
      let box2 = Box()
      box2.HalfExtents <- toVec3 (0.1, 0.4, 0.1)
      bx2.Box <- box2
      batchAdd s [ makeCompoundCmd (nextId "L-shape") (x, h + float i * 0.5, 0.0) [(bx1, (0.0, 0.0, 0.0)); (bx2, (0.3, 0.3, 0.0))] 2.0
                   |> withColorAndMaterial (Some (makeColor 0.2 (0.4 + float i * 0.12) 0.9 1.0)) None ]
    for i in 0 .. 3 do
      let x = float i * 2.5 - 3.5
      let bar = Shape()
      let b1 = Box()
      b1.HalfExtents <- toVec3 (0.5, 0.08, 0.08)
      bar.Box <- b1
      let stem = Shape()
      let b2 = Box()
      b2.HalfExtents <- toVec3 (0.08, 0.35, 0.08)
      stem.Box <- b2
      batchAdd s [ makeCompoundCmd (nextId "T-shape") (x, h + 3.0 + float i * 0.5, 0.0) [(bar, (0.0, 0.35, 0.0)); (stem, (0.0, 0.0, 0.0))] 2.5
                   |> withColorAndMaterial (Some (makeColor 0.9 (0.3 + float i * 0.15) 0.2 1.0)) None ]
    for i in 0 .. 3 do
      let x = float i * 2.0 - 3.0
      let ds1 = Shape()
      let dsp1 = Sphere()
      dsp1.Radius <- 0.2
      ds1.Sphere <- dsp1
      let ds2 = Shape()
      let dsp2 = Sphere()
      dsp2.Radius <- 0.2
      ds2.Sphere <- dsp2
      batchAdd s [ makeCompoundCmd (nextId "dumbbell") (x, h + 6.0, float i * 0.3 - 0.5) [(ds1, (-0.35, 0.0, 0.0)); (ds2, (0.35, 0.0, 0.0))] 3.0
                   |> withColorAndMaterial (Some (makeColor (float i * 0.25) 0.8 0.3 1.0)) None ]
    printfn "  Dropping L-shapes, T-shapes, and dumbbells..."
    runFor s 5.0
    setCamera s (3.0, 2.0, 6.0) (0.0, 1.0, 0.0) |> ignore
    runFor s 2.0
    listBodies s

runStandalone "Demo 20: Compound Constructions" run
