// AllDemos.fsx — All 24 demos (15 original + 3 constraint/query/kinematic + 3 shape demos + camera showcase + 2 mesh terrain)
// Loaded by RunAll.fsx and AutoRun.fsx

#load "Prelude.fsx"

open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets
open PhysicsClient.Generators
open PhysicsClient.Steering
open PhysicsClient.StateDisplay
open PhysicsSandbox.Shared.Contracts

type Demo = { Name: string; Description: string; Run: Session -> unit }

// Inline constraint builders (weld + distance limit not in Prelude)
let makeWeldInline (id: string) (bodyA: string) (bodyB: string) =
    let w = WeldConstraint()
    w.LocalOffset <- toVec3 (0.0, 0.0, 0.0)
    let orient = Vec4()
    orient.W <- 1.0
    w.LocalOrientation <- orient
    let spring = SpringSettings()
    spring.Frequency <- 30.0
    spring.DampingRatio <- 1.0
    w.Spring <- spring
    let ct = ConstraintType()
    ct.Weld <- w
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

let makeDistanceLimitInline (id: string) (bodyA: string) (bodyB: string) (minDist: float) (maxDist: float) =
    let dl = DistanceLimitConstraint()
    dl.LocalOffsetA <- toVec3 (0.0, 0.0, 0.0)
    dl.LocalOffsetB <- toVec3 (0.0, 0.0, 0.0)
    dl.MinDistance <- minDist
    dl.MaxDistance <- maxDist
    let spring = SpringSettings()
    spring.Frequency <- 30.0
    spring.DampingRatio <- 1.0
    dl.Spring <- spring
    let ct = ConstraintType()
    ct.DistanceLimit <- dl
    let ac = AddConstraint()
    ac.Id <- id
    ac.BodyA <- bodyA
    ac.BodyB <- bodyB
    ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

let demos = [|

  // Demo 01: Hello Drop
  { Name = "Hello Drop"
    Description = "Six different shapes fall side by side — spheres, boxes, capsule, cylinder with bouncy/sticky materials."
    Run = fun s ->
      resetSimulation s
      setCamera s (6.0, 5.0, 8.0) (0.0, 3.0, 0.0) |> ignore
      setDemoInfo s "Demo 01: Hello Drop" "Six different shapes fall side by side — spheres, boxes, capsule, cylinder with bouncy/sticky materials."
      let dropHeight = 10.0
      let ballId = nextId "sphere"
      batchAdd s [ makeSphereCmd ballId (-3.0, dropHeight, 0.0) 0.11 6.35
                   |> withColorAndMaterial (Some projectileColor) (Some stickyMaterial) ]
      let beachId = nextId "sphere"
      batchAdd s [ makeSphereCmd beachId (-1.0, dropHeight, 0.0) 0.2 0.1
                   |> withColorAndMaterial (Some accentYellow) (Some bouncyMaterial) ]
      let crateId = nextId "box"
      batchAdd s [ makeBoxCmd crateId (1.0, dropHeight, 0.0) (0.25, 0.25, 0.25) 20.0
                   |> withColorAndMaterial (Some targetColor) None ]
      let dieId = nextId "box"
      batchAdd s [ makeBoxCmd dieId (2.5, dropHeight, 0.0) (0.05, 0.05, 0.05) 0.03
                   |> withColorAndMaterial (Some accentPurple) None ]
      let capId = nextId "capsule"
      batchAdd s [ makeCapsuleCmd capId (4.0, dropHeight, 0.0) 0.12 0.4 3.0
                   |> withColorAndMaterial (Some accentGreen) None ]
      let cylId = nextId "cylinder"
      batchAdd s [ makeCylinderCmd cylId (5.5, dropHeight, 0.0) 0.15 0.5 4.0
                   |> withColorAndMaterial (Some accentOrange) None ]
      let triId = nextId "triangle"
      batchAdd s [ makeTriangleCmd triId (7.0, dropHeight, 0.0) (-0.3, -0.3, -0.3) (0.3, -0.3, -0.3) (0.0, 0.3, 0.3) 1.0
                   |> withColorAndMaterial (Some kinematicColor) None ]
      let hullId = nextId "hull"
      let octaPts = [(0.3,0.0,0.0);(-0.3,0.0,0.0);(0.0,0.3,0.0);(0.0,-0.3,0.0);(0.0,0.0,0.3);(0.0,0.0,-0.3)]
      batchAdd s [ makeConvexHullCmd hullId (8.5, dropHeight, 0.0) octaPts 2.0
                   |> withColorAndMaterial (Some accentPurple) None ]
      printfn "  Dropping: ball, beach ball, crate, die, capsule, cylinder, triangle, octahedron — all from %.0fm" dropHeight
      printfn "  Bouncy beach ball vs sticky bowling ball..."
      runFor s 2.5
      setCamera s (4.0, 1.0, 5.0) (1.0, 0.2, 0.0) |> ignore
      printfn "  Ground-level view — notice different resting positions"
      runFor s 1.5
      let impulseCmds = [ for id in [beachId; crateId; dieId; capId; cylId] do makeImpulseCmd id (0.0, 5.0, 0.0) ]
      batchAdd s impulseCmds
      printfn "  Upward impulse applied — watch the light ones fly!"
      setCamera s (6.0, 5.0, 8.0) (0.0, 3.0, 0.0) |> ignore
      runFor s 3.0
      listBodies s }

  // Demo 02: Bouncing Marbles
  { Name = "Bouncing Marbles"
    Description = "Two color-coded waves of marbles — bouncing, colliding, settling."
    Run = fun s ->
      resetSimulation s
      setCamera s (5.0, 8.0, 5.0) (0.0, 1.0, 0.0) |> ignore
      setDemoInfo s "Demo 02: Bouncing Marbles" "Two color-coded waves of marbles — bouncing, colliding, settling."
      let rng = System.Random(42)
      let wave1 =
          [ for i in 0 .. 14 do
              let x = float (i % 5) * 0.4 - 0.8
              let z = float (i / 5) * 0.4 - 0.4
              let y = 5.0 + rng.NextDouble() * 5.0
              let radius = 0.05 + rng.NextDouble() * 0.15
              let mass = radius * radius * 10.0
              makeSphereCmd (nextId "sphere") (x, y, z) radius mass
              |> withColorAndMaterial (Some accentYellow) None ]
      batchAdd s wave1
      printfn "  Wave 1: 15 yellow marbles raining down..."
      runFor s 3.0
      setCamera s (3.0, 4.0, 3.0) (0.0, 0.5, 0.0) |> ignore
      let wave2 =
          [ for i in 0 .. 9 do
              let x = rng.NextDouble() * 1.6 - 0.8
              let z = rng.NextDouble() * 1.6 - 0.8
              let y = 8.0 + rng.NextDouble() * 3.0
              let radius = 0.08 + rng.NextDouble() * 0.12
              let mass = radius * radius * 10.0
              makeSphereCmd (nextId "sphere") (x, y, z) radius mass
              |> withColorAndMaterial (Some accentGreen) None ]
      batchAdd s wave2
      printfn "  Wave 2: 10 green marbles into the pile!"
      runFor s 3.0
      printfn "  Settled."
      sleep 1000
      listBodies s }

  // Demo 03: Crate Stack
  { Name = "Crate Stack"
    Description = "A 12-crate tower hit by a boulder — dramatic toppling with colors."
    Run = fun s ->
      resetSimulation s
      setCamera s (8.0, 7.0, 4.0) (0.0, 5.0, 0.0) |> ignore
      setDemoInfo s "Demo 03: Crate Stack" "A 12-crate tower hit by a boulder — dramatic toppling with colors."
      // Build tower of colored crates
      let towerCmds =
          [ for i in 0..11 do
              let y = 0.5 + float i * 1.0
              makeBoxCmd (nextId "box") (0.0, y, 0.0) (0.5, 0.5, 0.5) 2.0
              |> withColorAndMaterial (Some targetColor) None ]
      batchAdd s towerCmds
      // Add compound bodies (2 welded boxes each) on top of the stack
      for i in 0 .. 1 do
        let y = 12.5 + float i * 1.2
        let bx1 = Shape()
        let box1 = Box()
        box1.HalfExtents <- toVec3 (0.4, 0.15, 0.15)
        bx1.Box <- box1
        let bx2 = Shape()
        let box2 = Box()
        box2.HalfExtents <- toVec3 (0.15, 0.4, 0.15)
        bx2.Box <- box2
        batchAdd s [ makeCompoundCmd (nextId "compound") (0.0, y, 0.0) [(bx1, (0.0, 0.0, 0.0)); (bx2, (0.25, 0.25, 0.0))] 3.0
                     |> withColorAndMaterial (Some accentPurple) None ]
      printfn "  Built tower of 12 crates + 2 compound caps"
      runFor s 2.0
      // Boulder spawned close, aimed at tower center of mass
      let ballId = nextId "sphere"
      batchAdd s [ makeSphereCmd ballId (-4.0, 3.0, 0.0) 0.5 200.0
                   |> withColorAndMaterial (Some projectileColor) None ]
      printfn "  Boulder ready — aiming at tower center..."
      sleep 500
      printfn "  STRIKE!"
      launch s ballId (0.0, 3.0, 0.0) 40.0 |> ignore
      runFor s 2.0
      setCamera s (5.0, 3.0, 5.0) (0.0, 2.0, 0.0) |> ignore
      printfn "  Watching debris settle..."
      runFor s 2.5
      setCamera s (0.0, 12.0, 0.1) (0.0, 0.0, 0.0) |> ignore
      printfn "  Overhead view of destruction"
      sleep 1500
      listBodies s }

  // Demo 04: Bowling Alley
  { Name = "Bowling Alley"
    Description = "Launch a bowling ball head-on at a pyramid of bricks — frontal impact."
    Run = fun s ->
      resetSimulation s
      setCamera s (0.0, 4.0, -6.0) (0.0, 1.0, 5.0) |> ignore
      setDemoInfo s "Demo 04: Bowling Alley" "Launch a bowling ball head-on at a pyramid of bricks — frontal impact."
      // Pyramid placed along Z-axis so ball approaches frontally
      let brickCmds =
          [ for layer in 0..3 do
              let count = 4 - layer
              let y = 0.5 + float layer * 1.0
              let xOff = -(float (count - 1)) * 0.5
              for col in 0..count-1 do
                  let x = xOff + float col * 1.0
                  makeBoxCmd (nextId "box") (x, y, 5.0) (0.5, 0.5, 0.5) 2.0
                  |> withColorAndMaterial (Some targetColor) None ]
      batchAdd s brickCmds
      // Replace one pin position with a convex hull diamond/octahedron
      let diamondPts = [(0.0,0.5,0.0);(0.0,-0.5,0.0);(0.3,0.0,0.0);(-0.3,0.0,0.0);(0.0,0.0,0.3);(0.0,0.0,-0.3)]
      batchAdd s [ makeConvexHullCmd (nextId "hull") (2.0, 0.5, 5.0) diamondPts 2.0
                   |> withColorAndMaterial (Some accentPurple) None ]
      printfn "  Built pyramid (4 layers) + 1 convex hull pin at Z=5"
      let ballId = nextId "sphere"
      batchAdd s [ makeSphereCmd ballId (0.0, 0.5, -2.0) 0.4 10.0
                   |> withColorAndMaterial (Some projectileColor) None ]
      printfn "  Bowling ball at Z=-2, aimed head-on"
      runFor s 1.0
      printfn "  Admiring the pyramid..."
      sleep 1500
      printfn "  STRIKE! Launching ball..."
      launch s ballId (0.0, 1.0, 5.0) 150.0 |> ignore
      runFor s 2.5
      setCamera s (3.0, 1.0, 3.0) (0.0, 0.5, 5.0) |> ignore
      printfn "  Low-angle debris view"
      runFor s 2.0
      listBodies s }

  // Demo 05: Marble Rain
  { Name = "Marble Rain"
    Description = "50 mixed shapes rain from the sky — spheres, crates, capsules, cylinders."
    Run = fun s ->
      resetSimulation s
      setCamera s (6.0, 10.0, 6.0) (0.0, 0.0, 0.0) |> ignore
      setDemoInfo s "Demo 05: Marble Rain" "50 mixed shapes rain from the sky — spheres, crates, capsules, cylinders."
      let rng = System.Random(42)
      let wave1 =
          [ for i in 0 .. 19 do
              let x = rng.NextDouble() * 8.0 - 4.0
              let z = rng.NextDouble() * 8.0 - 4.0
              let y = 5.0 + rng.NextDouble() * 5.0
              let radius = 0.1 + rng.NextDouble() * 0.2
              makeSphereCmd (nextId "sphere") (x, y, z) radius (radius * 5.0)
              |> withColorAndMaterial (Some accentYellow) None ]
      batchAdd s wave1
      printfn "  Wave 1: 20 yellow spheres raining down..."
      runFor s 3.0
      let rng2 = System.Random(99)
      let wave2 =
          [ for i in 0 .. 4 do
                let x = rng2.NextDouble() * 4.0 - 2.0
                let z = rng2.NextDouble() * 4.0 - 2.0
                let y = 8.0 + rng2.NextDouble() * 5.0
                makeBoxCmd (nextId "box") (x, y, z) (0.15, 0.15, 0.15) 3.0
                |> withColorAndMaterial (Some targetColor) None
            for i in 0 .. 4 do
                let x = rng2.NextDouble() * 4.0 - 2.0
                let z = rng2.NextDouble() * 4.0 - 2.0
                let y = 9.0 + rng2.NextDouble() * 4.0
                makeCapsuleCmd (nextId "capsule") (x, y, z) 0.1 0.4 2.0
                |> withColorAndMaterial (Some accentGreen) None
            for i in 0 .. 4 do
                let x = rng2.NextDouble() * 3.0 - 1.5
                let z = rng2.NextDouble() * 3.0 - 1.5
                let y = 10.0 + rng2.NextDouble() * 4.0
                makeCylinderCmd (nextId "cylinder") (x, y, z) 0.12 0.3 2.5
                |> withColorAndMaterial (Some accentOrange) None
            for i in 0 .. 4 do
                let x = rng2.NextDouble() * 3.0 - 1.5
                let z = rng2.NextDouble() * 3.0 - 1.5
                let y = 11.0 + rng2.NextDouble() * 3.0
                makeBoxCmd (nextId "box") (x, y, z) (0.05, 0.05, 0.05) 0.03
                |> withColorAndMaterial (Some accentPurple) None ]
      batchAdd s wave2
      printfn "  Wave 2: 5 crates + 5 capsules + 5 cylinders + 5 dice!"
      setCamera s (4.0, 6.0, 4.0) (0.0, 1.0, 0.0) |> ignore
      runFor s 4.0
      setCamera s (2.5, 0.8, 2.5) (0.0, 0.5, 0.0) |> ignore
      printfn "  Close-up of the mixed-shape pile"
      sleep 1500
      listBodies s }

  // Demo 06: Domino Row
  { Name = "Domino Row"
    Description = "A row of 20 brick dominoes toppled by a push."
    Run = fun s ->
      resetSimulation s
      setCamera s (-2.0, 3.0, 6.0) (5.0, 0.5, 0.0) |> ignore
      setDemoInfo s "Demo 06: Domino Row" "A row of 20 brick dominoes toppled by a push."
      let ids = [ for _ in 0..19 -> nextId "box" ]
      let cmds =
          [ for i in 0..19 do
              let x = float i * 0.5
              let t = float i / 19.0
              let c = makeColor (0.3 + t * 0.5) (0.6 - t * 0.2) (1.0 - t * 0.6) 1.0
              makeBoxCmd ids.[i] (x, 0.3, 0.0) (0.05, 0.3, 0.15) 1.0
              |> withColorAndMaterial (Some c) None ]
      batchAdd s cmds
      // Add 3 compound L-shaped domino pieces interspersed
      for i in 0 .. 2 do
        let x = float (5 + i * 6) * 0.5
        let bx1 = Shape()
        let b1 = Box()
        b1.HalfExtents <- toVec3 (0.05, 0.3, 0.15)
        bx1.Box <- b1
        let bx2 = Shape()
        let b2 = Box()
        b2.HalfExtents <- toVec3 (0.15, 0.05, 0.15)
        bx2.Box <- b2
        batchAdd s [ makeCompoundCmd (nextId "compound") (x, 0.3, 0.4) [(bx1, (0.0, 0.0, 0.0)); (bx2, (0.1, -0.25, 0.0))] 1.5
                     |> withColorAndMaterial (Some accentPurple) None ]
      let firstId = ids.[0]
      printfn "  Placed 20 dominoes + 3 compound L-shapes"
      runFor s 1.0
      printfn "  Toppling first domino..."
      push s firstId East 3.0 |> ignore
      setCamera s (0.0, 2.5, 4.0) (2.0, 0.3, 0.0) |> ignore
      runFor s 2.0
      setCamera s (3.0, 2.5, 4.0) (5.0, 0.3, 0.0) |> ignore
      runFor s 2.0
      setCamera s (6.0, 2.5, 4.0) (8.0, 0.3, 0.0) |> ignore
      runFor s 2.0
      setCamera s (12.0, 2.0, 4.0) (9.0, 0.0, 0.0) |> ignore
      sleep 500
      listBodies s }

  // Demo 07: Spinning Tops
  { Name = "Spinning Tops"
    Description = "Six spinning objects collide in the center — angular momentum chaos."
    Run = fun s ->
      resetSimulation s
      setCamera s (0.0, 10.0, 8.0) (0.0, 0.5, 0.0) |> ignore
      setDemoInfo s "Demo 07: Spinning Tops" "Six spinning objects collide in the center — angular momentum chaos."
      let radius = 2.0
      let ids =
          [ for i in 0 .. 5 do
              if i % 2 = 0 then nextId "sphere" else nextId "box" ]
      let bodyCmds =
          [ for i in 0 .. 5 do
              let angle = float i * System.Math.PI / 3.0
              let x = radius * cos angle
              let z = radius * sin angle
              match i % 3 with
              | 0 -> makeSphereCmd ids.[i] (x, 0.3, z) 0.25 2.0
                     |> withColorAndMaterial (Some accentYellow) None
              | 1 -> makeCapsuleCmd ids.[i] (x, 0.4, z) 0.15 0.4 3.0
                     |> withColorAndMaterial (Some accentGreen) None
              | _ -> makeCylinderCmd ids.[i] (x, 0.4, z) 0.2 0.3 4.0
                     |> withColorAndMaterial (Some accentOrange) None ]
      batchAdd s bodyCmds
      printfn "  6 objects: spheres, capsules, cylinders in a ring"
      runFor s 0.5
      batchAdd s [
          makeTorqueCmd ids.[0] (0.0, 80.0, 0.0)
          makeTorqueCmd ids.[1] (0.0, -60.0, 30.0)
          makeTorqueCmd ids.[2] (0.0, 70.0, 0.0)
          makeTorqueCmd ids.[3] (40.0, 0.0, -50.0)
          makeTorqueCmd ids.[4] (0.0, -90.0, 0.0)
          makeTorqueCmd ids.[5] (-30.0, 60.0, 0.0) ]
      printfn "  All spinning..."
      runFor s 2.0
      wireframe s true |> ignore
      printfn "  Wireframe on — pushing inward!"
      let impulseCmds =
          [ for i in 0 .. 5 do
              let angle = float i * System.Math.PI / 3.0
              makeImpulseCmd ids.[i] (-cos angle * 8.0, 0.5, -sin angle * 8.0) ]
      batchAdd s impulseCmds
      setCamera s (5.0, 3.0, 5.0) (0.0, 0.5, 0.0) |> ignore
      runFor s 3.0
      wireframe s false |> ignore
      setCamera s (4.0, 2.0, 4.0) (0.0, 0.3, 0.0) |> ignore
      printfn "  Settling..."
      runFor s 2.0
      listBodies s }

  // Demo 08: Gravity Flip
  { Name = "Gravity Flip"
    Description = "Light objects under four gravity directions — up, sideways, diagonal, restored."
    Run = fun s ->
      resetSimulation s
      setCamera s (6.0, 5.0, 6.0) (0.0, 2.0, 0.0) |> ignore
      setDemoInfo s "Demo 08: Gravity Flip" "Light objects under four gravity directions — up, sideways, diagonal, restored."
      let rng = System.Random(77)
      // Octahedron vertices for convex hull
      let octaPoints = [(1.0,0.0,0.0);(-1.0,0.0,0.0);(0.0,1.0,0.0);(0.0,-1.0,0.0);(0.0,0.0,1.0);(0.0,0.0,-1.0)]
      let bodyCmds =
          [ for i in 0 .. 5 do
                let x = float (i % 3) * 1.2 - 1.2
                let z = float (i / 3) * 1.2 - 0.6
                let y = 3.0 + rng.NextDouble() * 4.0
                makeSphereCmd (nextId "sphere") (x, y, z) 0.2 0.1
                |> withColorAndMaterial (Some accentYellow) None
            for i in 0 .. 7 do
                let x = float (i % 4) * 0.8 - 1.2
                let z = float (i / 4) * 0.8 - 0.4
                let y = 5.0 + rng.NextDouble() * 3.0
                makeBoxCmd (nextId "box") (x, y, z) (0.05, 0.05, 0.05) 0.03
                |> withColorAndMaterial (Some targetColor) None
            // Capsules
            for i in 0 .. 2 do
                let x = float i * 1.5 - 1.5
                makeCapsuleCmd (nextId "capsule") (x, 1.5, 1.5) 0.1 0.3 1.0
                |> withColorAndMaterial (Some accentGreen) None
            // Cylinders
            for i in 0 .. 1 do
                let x = float i * 2.0 - 1.0
                makeCylinderCmd (nextId "cylinder") (x, 2.0, -1.5) 0.12 0.25 1.5
                |> withColorAndMaterial (Some accentOrange) None
            // Triangle "ramp" shapes
            for i in 0 .. 1 do
                let x = float i * 3.0 - 1.5
                makeTriangleCmd (nextId "triangle") (x, 4.0, 0.0) (-0.3, -0.3, -0.3) (0.3, -0.3, -0.3) (0.0, 0.3, 0.3) 0.5
                |> withColorAndMaterial (Some projectileColor) None
            // Convex hull "octahedron" shapes
            for i in 0 .. 1 do
                let x = float i * 2.5 - 1.25
                let pts = octaPoints |> List.map (fun (a,b,c) -> (a*0.2, b*0.2, c*0.2))
                makeConvexHullCmd (nextId "hull") (x, 6.0, 0.5) pts 1.0
                |> withColorAndMaterial (Some accentPurple) None ]
      batchAdd s bodyCmds
      printfn "  Mixed shapes: spheres, boxes, capsules, cylinders, triangles, convex hulls"
      runFor s 2.5
      printfn "  GRAVITY UP!"
      setCamera s (5.0, 1.0, 5.0) (0.0, 6.0, 0.0) |> ignore
      setGravity s (0.0, 15.0, 0.0) |> ignore
      runFor s 2.0
      printfn "  GRAVITY EAST!"
      setGravity s (12.0, 0.0, 0.0) |> ignore
      setCamera s (-8.0, 4.0, 4.0) (2.0, 2.0, 0.0) |> ignore
      runFor s 2.0
      printfn "  GRAVITY DIAGONAL!"
      setGravity s (-8.0, -5.0, 8.0) |> ignore
      setCamera s (4.0, 6.0, -6.0) (-2.0, 1.0, 2.0) |> ignore
      runFor s 2.0
      printfn "  Gravity restored!"
      setGravity s (0.0, -9.81, 0.0) |> ignore
      setCamera s (6.0, 5.0, 6.0) (0.0, 1.0, 0.0) |> ignore
      runFor s 2.5
      listBodies s }

  // Demo 09: Billiards
  { Name = "Billiards"
    Description = "Cue ball breaks a triangle formation."
    Run = fun s ->
      resetSimulation s
      setCamera s (0.0, 10.0, 0.1) (0.0, 0.0, 0.0) |> ignore
      setDemoInfo s "Demo 09: Billiards" "Cue ball breaks a triangle formation."
      let r = 0.1
      let spacing = 0.22
      let cueId = "cue"
      let ballColors = [| accentYellow; accentGreen; accentOrange; accentPurple |]
      let cmds = [
          let mutable idx = 0
          for row in 0..4 do
            for col in 0..row do
              let x = float row * spacing * 0.866 + 1.0
              let z = (float col - float row / 2.0) * spacing
              let c = ballColors.[idx % 4]
              idx <- idx + 1
              makeSphereCmd (nextId "sphere") (x, r, z) r 0.17
              |> withColorAndMaterial (Some c) (Some slipperyMaterial)
          makeSphereCmd cueId (-2.0, r, 0.0) (r * 1.1) 0.17
          |> withColorAndMaterial (Some projectileColor) (Some slipperyMaterial) ]
      batchAdd s cmds
      // Add a convex hull "diamond ball" alongside the regular spheres
      let diamondPts = [(0.0,r*1.2,0.0);(0.0,-r*1.2,0.0);(r,0.0,0.0);(-r,0.0,0.0);(0.0,0.0,r);(0.0,0.0,-r)]
      batchAdd s [ makeConvexHullCmd (nextId "hull") (0.5, r, -1.0) diamondPts 0.17
                   |> withColorAndMaterial (Some accentPurple) (Some slipperyMaterial) ]
      printfn "  15 balls + cue + diamond hull placed"
      printfn "  Admiring the formation..."
      sleep 1500
      setCamera s (-3.0, 1.5, 2.0) (1.0, 0.0, 0.0) |> ignore
      runFor s 0.5
      printfn "  BREAK!"
      launch s cueId (1.5, 0.0, 0.0) 15.0 |> ignore
      setCamera s (-1.0, 0.4, 1.5) (1.0, 0.1, 0.0) |> ignore
      runFor s 2.0
      setCamera s (0.0, 5.0, 3.0) (0.0, 0.0, 0.0) |> ignore
      runFor s 2.0
      setCamera s (0.0, 8.0, 0.1) (0.0, 0.0, 0.0) |> ignore
      sleep 1000
      listBodies s }

  // Demo 10: Chaos Scene
  { Name = "Chaos Scene"
    Description = "Everything: presets, generators, steering, gravity, camera sweeps — with colors and cylinders."
    Run = fun s ->
      resetSimulation s
      setCamera s (12.0, 8.0, 12.0) (0.0, 2.0, 0.0) |> ignore
      setDemoInfo s "Demo 10: Chaos Scene" "Everything: presets, generators, steering, gravity, camera sweeps — with colors and cylinders."
      printfn "  Act 1: Building the stage..."
      pyramid s 5 (Some (-4.0, 0.0, 0.0)) |> ok |> ignore
      stack s 6 (Some (4.0, 0.0, 0.0)) |> ok |> ignore
      row s 8 (Some (-3.0, 0.0, 3.0)) |> ok |> ignore
      // Add cylinder "pillars"
      batchAdd s [
          for i in 0..2 do
              let x = float i * 3.0 - 3.0
              makeCylinderCmd (nextId "cylinder") (x, 1.0, -3.0) 0.2 2.0 10.0
              |> withColorAndMaterial (Some accentOrange) None ]
      // Add triangle bodies and convex hulls to the mix
      batchAdd s [
          for i in 0..2 do
              let x = float i * 2.5 - 2.5
              makeTriangleCmd (nextId "triangle") (x, 3.0, 4.0) (-0.3, -0.2, -0.2) (0.3, -0.2, -0.2) (0.0, 0.3, 0.2) 1.0
              |> withColorAndMaterial (Some kinematicColor) None ]
      let chaosOctaPts = [(0.25,0.0,0.0);(-0.25,0.0,0.0);(0.0,0.25,0.0);(0.0,-0.25,0.0);(0.0,0.0,0.25);(0.0,0.0,-0.25)]
      batchAdd s [
          for i in 0..1 do
              let x = float i * 3.0 - 1.5
              makeConvexHullCmd (nextId "hull") (x, 4.0, -4.0) chaosOctaPts 2.0
              |> withColorAndMaterial (Some accentPurple) None ]
      runFor s 1.5
      sleep 800
      printfn "  Act 2: Bombardment!"
      setCamera s (0.0, 15.0, 10.0) (0.0, 2.0, 0.0) |> ignore
      let projCmds =
          [ for i in 0..9 do
              let x = float (i % 5) * 2.0 - 4.0
              let z = float (i / 5) * 2.0 - 1.0
              makeSphereCmd (nextId "sphere") (x, 12.0 + float i * 0.5, z) 0.25 2.0
              |> withColorAndMaterial (Some projectileColor) None ]
      batchAdd s projCmds
      let projIds = [ for i in 0..9 -> sprintf "sphere-%d" (i + 1) ]
      batchAdd s [ for id in projIds do makeImpulseCmd id (0.0, -20.0, 0.0) ]
      runFor s 3.0
      printfn "  Act 3: Boulder attack on pyramid!"
      setCamera s (-10.0, 3.0, 0.0) (-4.0, 2.0, 0.0) |> ignore
      let rockId = nextId "sphere"
      batchAdd s [ makeSphereCmd rockId (-8.0, 2.0, 0.0) 0.5 200.0
                   |> withColorAndMaterial (Some projectileColor) None ]
      launch s rockId (-4.0, 2.0, 0.0) 35.0 |> ignore
      runFor s 3.0
      printfn "  Act 4: Gravity chaos!"
      setCamera s (8.0, 2.0, 8.0) (0.0, 3.0, 0.0) |> ignore
      setGravity s (0.0, 8.0, 0.0) |> ignore
      runFor s 2.0
      setGravity s (5.0, 0.0, 5.0) |> ignore
      setCamera s (-6.0, 4.0, -6.0) (2.0, 2.0, 2.0) |> ignore
      runFor s 2.0
      setGravity s (0.0, -9.81, 0.0) |> ignore
      printfn "  Act 5: Camera sweep"
      setCamera s (10.0, 6.0, 10.0) (0.0, 1.0, 0.0) |> ignore
      wireframe s true |> ignore
      runFor s 2.0
      wireframe s false |> ignore
      for angle in 0..6 do
        let a = float angle * 0.9
        setCamera s (10.0 * cos a, 5.0, 10.0 * sin a) (0.0, 1.0, 0.0) |> ignore
        sleep 300
      printfn "  Chaos complete!"
      status s }

  // Demo 11: Body Scaling
  { Name = "Body Scaling"
    Description = "Progressive body count with tight packing — collision-dense stress test."
    Run = fun s ->
      resetSimulation s
      setDemoInfo s "Demo 11: Body Scaling" "Progressive body count with tight packing — collision-dense stress test."
      let tiers = [50; 100; 200; 500]
      for tier in tiers do
        printfn "  === Tier: %d bodies ===" tier
        resetSimulation s
        let dist = if tier <= 100 then 10.0 elif tier <= 200 then 18.0 else 30.0
        setCamera s (dist, dist * 0.6, dist) (0.0, 2.0, 0.0) |> ignore
        let rng = System.Random(tier)
        timed (sprintf "Tier %d setup" tier) (fun () ->
            let cols = int (sqrt (float tier))
            let cmds =
                [ for i in 0 .. tier - 1 do
                    let x = float (i % cols) * 0.7 - float cols * 0.35
                    let z = float ((i / cols) % cols) * 0.7 - float cols * 0.35
                    let y = 2.0 + float (i / (cols * cols)) * 1.5
                    let m = 0.5 + rng.NextDouble() * 2.0
                    match i % 5 with
                    | 0 -> makeBoxCmd (nextId "box") (x, y, z) (0.2, 0.2, 0.2) m
                           |> withColorAndMaterial (Some targetColor) None
                    | 1 -> makeSphereCmd (nextId "sphere") (x, y, z) (0.15 + rng.NextDouble() * 0.1) m
                           |> withColorAndMaterial (Some accentYellow) None
                    | 2 -> makeCapsuleCmd (nextId "capsule") (x, y, z) 0.1 0.3 m
                           |> withColorAndMaterial (Some accentGreen) None
                    | 3 -> makeCylinderCmd (nextId "cylinder") (x, y, z) 0.12 0.25 m
                           |> withColorAndMaterial (Some accentOrange) None
                    | _ ->
                        let s1 = Shape()
                        let sp1 = Sphere()
                        sp1.Radius <- 0.12
                        s1.Sphere <- sp1
                        let s2 = Shape()
                        let sp2 = Sphere()
                        sp2.Radius <- 0.12
                        s2.Sphere <- sp2
                        makeCompoundCmd (nextId "compound") (x, y, z) [(s1, (-0.2, 0.0, 0.0)); (s2, (0.2, 0.0, 0.0))] m
                        |> withColorAndMaterial (Some accentPurple) None ]
            batchAdd s cmds)
        timed (sprintf "Tier %d simulation (3s)" tier) (fun () ->
            runFor s 3.0)
        printfn "  Tier %d complete" tier
      printfn "  All tiers complete — check [TIME] markers for degradation"
      status s }

  // Demo 12: Collision Pit
  { Name = "Collision Pit"
    Description = "Three waves of varied spheres dropped into a walled pit — maximum collision density."
    Run = fun s ->
      resetSimulation s
      setCamera s (8.0, 10.0, 8.0) (0.0, 2.0, 0.0) |> ignore
      setDemoInfo s "Demo 12: Collision Pit" "Three waves of varied spheres dropped into a walled pit — maximum collision density."
      timed "Pit walls setup" (fun () ->
          let wallCmds = [
              makeBoxCmd (nextId "box") (0.0, 2.0, -2.1) (2.0, 2.0, 0.1) 0.0
              makeBoxCmd (nextId "box") (0.0, 2.0, 2.1)  (2.0, 2.0, 0.1) 0.0
              makeBoxCmd (nextId "box") (-2.1, 2.0, 0.0) (0.1, 2.0, 2.0) 0.0
              makeBoxCmd (nextId "box") (2.1, 2.0, 0.0)  (0.1, 2.0, 2.0) 0.0 ]
          batchAdd s wallCmds)
      printfn "  Pit built (4x4m walled enclosure)"
      let rng = System.Random(55)
      // Tetrahedron points for convex hull
      let tetraPoints = [(0.0,0.3,0.0);(0.2,-0.1,0.0);(-0.1,-0.1,0.17);(-0.1,-0.1,-0.17)]
      timed "Wave 1 — 40 large spheres" (fun () ->
          let wave1 =
              [ for i in 0 .. 39 do
                  let x = float (i % 8) * 0.45 - 1.6
                  let z = float (i / 8) * 0.45 - 0.9
                  let y = 6.0 + rng.NextDouble() * 2.0
                  makeSphereCmd (nextId "sphere") (x, y, z) (0.15 + rng.NextDouble() * 0.1) (0.3 + rng.NextDouble() * 1.0)
                  |> withColorAndMaterial (Some accentYellow) None ]
          batchAdd s wave1)
      printfn "  Wave 1: 40 yellow spheres dropping..."
      runFor s 3.0
      setCamera s (5.0, 8.0, 5.0) (0.0, 3.0, 0.0) |> ignore
      timed "Wave 2 — 40 marbles + 10 convex hulls" (fun () ->
          let wave2 =
              [ for i in 0 .. 39 do
                  let x = rng.NextDouble() * 3.2 - 1.6
                  let z = rng.NextDouble() * 3.2 - 1.6
                  let y = 10.0 + rng.NextDouble() * 4.0
                  makeSphereCmd (nextId "sphere") (x, y, z) (0.06 + rng.NextDouble() * 0.06) (0.1 + rng.NextDouble() * 0.3)
                  |> withColorAndMaterial (Some accentGreen) None
                for i in 0 .. 9 do
                  let x = rng.NextDouble() * 2.8 - 1.4
                  let z = rng.NextDouble() * 2.8 - 1.4
                  let y = 11.0 + rng.NextDouble() * 3.0
                  makeConvexHullCmd (nextId "hull") (x, y, z) tetraPoints 0.8
                  |> withColorAndMaterial (Some accentPurple) None ]
          batchAdd s wave2)
      printfn "  Wave 2: 40 green marbles + 10 purple tetrahedra!"
      runFor s 4.0
      timed "Wave 3 — 15 heavy spheres + 5 compounds" (fun () ->
          let wave3 =
              [ for i in 0 .. 14 do
                  let x = rng.NextDouble() * 2.4 - 1.2
                  let z = rng.NextDouble() * 2.4 - 1.2
                  let y = 12.0 + rng.NextDouble() * 3.0
                  makeSphereCmd (nextId "sphere") (x, y, z) 0.2 3.0
                  |> withColorAndMaterial (Some projectileColor) None
                for i in 0 .. 4 do
                  let x = rng.NextDouble() * 2.0 - 1.0
                  let z = rng.NextDouble() * 2.0 - 1.0
                  let y = 13.0 + rng.NextDouble() * 2.0
                  let s1 = Shape()
                  let b1 = Box()
                  b1.HalfExtents <- toVec3 (0.1, 0.1, 0.1)
                  s1.Box <- b1
                  let s2 = Shape()
                  let b2 = Box()
                  b2.HalfExtents <- toVec3 (0.1, 0.1, 0.1)
                  s2.Box <- b2
                  makeCompoundCmd (nextId "compound") (x, y, z) [(s1, (0.15, 0.0, 0.0)); (s2, (-0.15, 0.0, 0.0))] 2.0
                  |> withColorAndMaterial (Some accentPurple) None ]
          batchAdd s wave3)
      printfn "  Wave 3: 15 red heavies + 5 purple compounds — IMPACT!"
      runFor s 4.0
      setCamera s (3.0, 2.0, 3.0) (0.0, 1.5, 0.0) |> ignore
      printfn "  Close-up of the overflowing pit"
      sleep 1500
      status s }

  // Demo 13: Force Frenzy
  { Name = "Force Frenzy"
    Description = "80 tightly-packed bodies hit with 3 rounds of escalating forces — collisions everywhere."
    Run = fun s ->
      resetSimulation s
      setCamera s (10.0, 8.0, 10.0) (0.0, 1.0, 0.0) |> ignore
      setDemoInfo s "Demo 13: Force Frenzy" "80 tightly-packed bodies hit with 3 rounds of escalating forces — collisions everywhere."
      let ids =
          timed "Create 80 bodies" (fun () ->
              let bodyIds = [ for _ in 0 .. 79 -> nextId "sphere" ]
              let cmds =
                  [ for idx in 0 .. 79 do
                      let x = float (idx % 8) * 0.7 - 2.45
                      let z = float (idx / 8) * 0.7 - 3.15
                      let mass = if idx % 3 = 0 then 0.5 else 1.5
                      let radius = if idx % 3 = 0 then 0.2 else 0.25
                      if idx < 40 then
                          makeSphereCmd bodyIds.[idx] (x, 0.5, z) radius mass
                          |> withColorAndMaterial (Some accentYellow) (Some bouncyMaterial)
                      else
                          makeSphereCmd bodyIds.[idx] (x, 0.5, z) radius mass
                          |> withColorAndMaterial (Some accentPurple) (Some stickyMaterial) ]
              batchAdd s cmds
              bodyIds)
      // Add triangle and convex hull projectiles as force targets
      batchAdd s [
          for i in 0..2 do
              let x = float i * 1.5 - 1.5
              makeTriangleCmd (nextId "triangle") (x, 0.5, 4.0) (-0.2, -0.2, -0.2) (0.2, -0.2, -0.2) (0.0, 0.2, 0.2) 0.8
              |> withColorAndMaterial (Some kinematicColor) (Some bouncyMaterial) ]
      let frenzyOctaPts = [(0.2,0.0,0.0);(-0.2,0.0,0.0);(0.0,0.2,0.0);(0.0,-0.2,0.0);(0.0,0.0,0.2);(0.0,0.0,-0.2)]
      batchAdd s [
          for i in 0..1 do
              let x = float i * 2.0 - 1.0
              makeConvexHullCmd (nextId "hull") (x, 0.5, -4.0) frenzyOctaPts 1.5
              |> withColorAndMaterial (Some accentPurple) (Some stickyMaterial) ]
      printfn "  80 spheres + 3 triangles + 2 convex hulls in tight grid"
      timed "Settle (1.5s)" (fun () -> runFor s 1.5)
      timed "Round 1 — upward impulses (3s)" (fun () ->
          batchAdd s [ for id in ids do makeImpulseCmd id (0.0, 12.0, 0.0) ]
          printfn "  Launch! Bodies colliding on the way up..."
          setCamera s (8.0, 2.0, 8.0) (0.0, 5.0, 0.0) |> ignore
          runFor s 3.0)
      timed "Round 2 — torques + sideways gravity (3s)" (fun () ->
          batchAdd s [ for id in ids do makeTorqueCmd id (0.0, 30.0, 15.0) ]
          setGravity s (10.0, -3.0, 0.0) |> ignore
          setCamera s (-12.0, 5.0, 8.0) (0.0, 2.0, 0.0) |> ignore
          printfn "  Spinning + sliding sideways..."
          runFor s 3.0)
      timed "Round 3 — inward impulses + low gravity (3s)" (fun () ->
          let inwardCmds =
              [ for idx in 0 .. 79 do
                  let x = float (idx % 8) * 0.7 - 2.45
                  let z = float (idx / 8) * 0.7 - 3.15
                  makeImpulseCmd ids.[idx] (-x * 3.0, 8.0, -z * 3.0) ]
          batchAdd s inwardCmds
          setGravity s (0.0, -2.0, 0.0) |> ignore
          setCamera s (0.0, 12.0, 10.0) (0.0, 3.0, 0.0) |> ignore
          printfn "  Swarming inward under low gravity!"
          runFor s 3.0)
      setGravity s (0.0, -9.81, 0.0) |> ignore
      printfn "  Gravity restored — settling..."
      setCamera s (8.0, 5.0, 8.0) (0.0, 1.0, 0.0) |> ignore
      runFor s 2.0
      status s }

  // Demo 14: Domino Cascade
  { Name = "Domino Cascade"
    Description = "120 dominoes in a semicircular path — chain reaction at scale."
    Run = fun s ->
      resetSimulation s
      setCamera s (0.0, 12.0, 0.1) (0.0, 0.0, 0.0) |> ignore
      setDemoInfo s "Demo 14: Domino Cascade" "120 dominoes in a semicircular path — chain reaction at scale."
      let count = 120
      let radius = 8.0
      let ids =
          timed (sprintf "Place %d dominoes" count) (fun () ->
              let dominoIds = [ for _ in 0 .. count - 1 -> nextId "box" ]
              let cmds =
                  [ for i in 0 .. count - 1 do
                      let angle = float i / float count * System.Math.PI
                      let x = radius * cos angle
                      let z = radius * sin angle
                      let t = float i / float (count - 1)
                      let c = makeColor (0.3 + t * 0.7) (0.6 - t * 0.4) (1.0 - t * 0.9) 1.0
                      // Rotate domino so thin axis aligns with tangent to arc
                      let halfA = -(System.Math.PI / 4.0 + angle / 2.0)
                      let qw = cos halfA
                      let qy = sin halfA
                      let orient = Vec4()
                      orient.X <- 0.0
                      orient.Y <- qy
                      orient.Z <- 0.0
                      orient.W <- qw
                      let cmd = makeBoxCmd dominoIds.[i] (x, 0.3, z) (0.05, 0.3, 0.15) 1.0
                                |> withColorAndMaterial (Some c) None
                      cmd.AddBody.Orientation <- orient
                      cmd ]
              batchAdd s cmds
              dominoIds)
      // Add compound L-shaped domino pieces at intervals along the arc
      for i in 0 .. 2 do
        let angle = float (20 + i * 40) / float count * System.Math.PI
        let x = radius * cos angle + 0.3
        let z = radius * sin angle
        let bx1 = Shape()
        let b1 = Box()
        b1.HalfExtents <- toVec3 (0.05, 0.3, 0.15)
        bx1.Box <- b1
        let bx2 = Shape()
        let b2 = Box()
        b2.HalfExtents <- toVec3 (0.15, 0.05, 0.15)
        bx2.Box <- b2
        batchAdd s [ makeCompoundCmd (nextId "compound") (x, 0.3, z) [(bx1, (0.0, 0.0, 0.0)); (bx2, (0.1, -0.25, 0.0))] 1.5
                     |> withColorAndMaterial (Some accentPurple) None ]
      printfn "  %d dominoes + 3 compound L-shapes in semicircle (radius %.0fm)" count radius
      runFor s 1.0
      // Brief overhead view to show the full semicircle layout
      setCamera s (0.0, 14.0, 0.1) (0.0, 0.0, 0.0) |> ignore
      printfn "  Overhead view — full semicircle"
      sleep 1000
      // Move to side view for the push
      setCamera s (radius + 2.0, 3.0, 0.0) (0.0, 0.5, 0.0) |> ignore
      printfn "  Pushing first domino..."
      // Tangent to circle at angle 0 is +Z direction
      pushVec s ids.[0] (0.0, 0.0, 5.0) |> ignore
      timed "Cascade propagation" (fun () ->
          runFor s 10.0)
      // Camera sweep along the cascade
      for i in 0..5 do
          let angle = float i / 5.0 * System.Math.PI
          let cx = (radius + 4.0) * cos angle
          let cz = (radius + 4.0) * sin angle
          setCamera s (cx, 3.0, cz) (0.0, 0.5, 0.0) |> ignore
          sleep 350
      printfn "  Cascade complete"
      status s }

  // Demo 15: Overload
  { Name = "Overload"
    Description = "Everything at once: 200+ bodies, forces, gravity shifts, camera sweep — stress ceiling test."
    Run = fun s ->
      resetSimulation s
      let totalSw = System.Diagnostics.Stopwatch.StartNew()
      setCamera s (20.0, 12.0, 20.0) (0.0, 2.0, 0.0) |> ignore
      setDemoInfo s "Demo 15: Overload" "Everything at once: 200+ bodies, forces, gravity shifts, camera sweep — stress ceiling test."
      // Act 1: Build formations
      let pyramidIds =
          timed "Act 1 — pyramid + stack + row" (fun () ->
              let pIds = pyramid s 7 (Some (-5.0, 0.0, 0.0)) |> ok
              stack s 10 (Some (5.0, 0.0, 0.0)) |> ok |> ignore
              row s 12 (Some (-5.0, 0.0, 5.0)) |> ok |> ignore
              runFor s 2.0
              pIds)
      // Act 2: Mixed shape rain
      let sphereIds =
          timed "Act 2 — 100 mixed shapes" (fun () ->
              let cmds =
                  [ for i in 0 .. 99 do
                      let x = float (i % 10) * 1.5 - 7.0
                      let z = float (i / 10) * 1.5 - 7.0
                      let y = 8.0 + float (i / 20) * 2.0
                      match i % 5 with
                      | 0 | 1 | 2 ->
                          makeSphereCmd (nextId "sphere") (x, y, z) 0.25 0.8
                          |> withColorAndMaterial (Some accentYellow) None
                      | 3 ->
                          makeCapsuleCmd (nextId "capsule") (x, y, z) 0.12 0.3 1.0
                          |> withColorAndMaterial (Some accentGreen) None
                      | _ ->
                          makeCylinderCmd (nextId "cylinder") (x, y, z) 0.15 0.25 1.2
                          |> withColorAndMaterial (Some accentOrange) None ]
              batchAdd s cmds
              let ids = cmds |> List.map (fun c -> c.AddBody.Id)
              runFor s 3.0
              ids)
      printfn "  200+ bodies active"
      status s
      // Act 3: Impulse storm
      timed "Act 3 — impulse storm" (fun () ->
          setCamera s (0.0, 20.0, 15.0) (0.0, 2.0, 0.0) |> ignore
          wireframe s true |> ignore
          let impCmds =
              [ for id in pyramidIds do makeImpulseCmd id (0.0, 10.0, 3.0) ]
          batchAdd s impCmds
          runFor s 3.0
          wireframe s false |> ignore)
      printfn "  Bodies after impulse storm:"
      status s
      // Act 4: Gravity chaos
      timed "Act 4 — gravity chaos" (fun () ->
          setCamera s (12.0, 3.0, 12.0) (0.0, 4.0, 0.0) |> ignore
          setGravity s (0.0, 10.0, 0.0) |> ignore
          runFor s 2.0
          setGravity s (6.0, 0.0, 6.0) |> ignore
          setCamera s (-12.0, 5.0, -12.0) (0.0, 3.0, 0.0) |> ignore
          runFor s 2.0
          setGravity s (0.0, -9.81, 0.0) |> ignore)
      // Act 5: Camera sweep + wireframe
      timed "Act 5 — camera sweep" (fun () ->
          wireframe s true |> ignore
          runFor s 1.0
          wireframe s false |> ignore
          for a in 0..7 do
              let angle = float a * 0.785
              setCamera s (18.0 * cos angle, 8.0, 18.0 * sin angle) (0.0, 2.0, 0.0) |> ignore
              sleep 400)
      totalSw.Stop()
      printfn "  [TIME] Total overload: %d ms" totalSw.ElapsedMilliseconds
      printfn "  Overload complete!"
      status s }

  // Demo 16: Constraints
  { Name = "Constraints"
    Description = "Pendulum chain, hinged bridge, and weld cluster — four constraint types in action."
    Run = fun s ->
      // Act 1: Pendulum Chain
      resetSimulation s
      setCamera s (0.0, 8.0, 10.0) (0.0, 5.0, 0.0) |> ignore
      setDemoInfo s "Demo 16: Constraints" "Pendulum chain, hinged bridge, and weld cluster — four constraint types in action."
      printfn "  Act 1: Pendulum Chain (ball-socket + distance-limit)"
      let anchorId = nextId "box"
      batchAdd s [ makeBoxCmd anchorId (0.0, 8.0, 0.0) (0.3, 0.1, 0.1) 0.0
                   |> withColorAndMaterial (Some structureColor) None ]
      let pendIds = [ for _ in 0..4 -> nextId "sphere" ]
      batchAdd s
          [ for i in 0..4 do
              let y = 7.0 - float i * 0.8
              let mat = if i = 4 then Some bouncyMaterial else None
              makeSphereCmd pendIds.[i] (0.0, y, 0.0) 0.2 1.0
              |> withColorAndMaterial (Some accentYellow) mat ]
      batchAdd s [
          makeBallSocketCmd (nextId "c") anchorId pendIds.[0] (0.0, -0.1, 0.0) (0.0, 0.2, 0.0)
          makeDistanceLimitInline (nextId "c") anchorId pendIds.[0] 0.3 0.6
          for i in 0..3 do
              makeBallSocketCmd (nextId "c") pendIds.[i] pendIds.[i+1] (0.0, -0.3, 0.0) (0.0, 0.3, 0.0)
              makeDistanceLimitInline (nextId "c") pendIds.[i] pendIds.[i+1] 0.3 0.6 ]
      runFor s 1.0
      batchAdd s [ makeImpulseCmd pendIds.[0] (5.0, 0.0, 0.0) ]
      printfn "  Pendulum disturbed — watching wave motion..."
      runFor s 4.0
      // Act 2: Hinged Bridge
      printfn "  Act 2: Hinged Bridge"
      resetSimulation s
      setCamera s (0.0, 5.0, 8.0) (0.0, 3.0, 0.0) |> ignore
      let pillarL = nextId "box"
      let pillarR = nextId "box"
      batchAdd s [
          makeBoxCmd pillarL (-3.5, 2.0, 0.0) (0.3, 2.0, 0.3) 0.0 |> withColorAndMaterial (Some structureColor) None
          makeBoxCmd pillarR (3.5, 2.0, 0.0) (0.3, 2.0, 0.3) 0.0 |> withColorAndMaterial (Some structureColor) None ]
      let plankIds = [ for _ in 0..5 -> nextId "box" ]
      batchAdd s
          [ for i in 0..5 do
              let x = -2.5 + float i * 1.0
              makeBoxCmd plankIds.[i] (x, 4.0, 0.0) (0.5, 0.05, 0.3) 2.0
              |> withColorAndMaterial (Some accentOrange) None ]
      batchAdd s [
          makeHingeCmd (nextId "c") pillarL plankIds.[0] (0.0, 0.0, 1.0) (0.3, 2.0, 0.0) (-0.5, 0.0, 0.0)
          for i in 0..4 do
              makeHingeCmd (nextId "c") plankIds.[i] plankIds.[i+1] (0.0, 0.0, 1.0) (0.5, 0.0, 0.0) (-0.5, 0.0, 0.0)
          makeHingeCmd (nextId "c") plankIds.[5] pillarR (0.0, 0.0, 1.0) (0.5, 0.0, 0.0) (-0.3, 2.0, 0.0) ]
      runFor s 1.0
      printfn "  Dropping weights on bridge..."
      batchAdd s [
          makeSphereCmd (nextId "sphere") (-1.0, 8.0, 0.0) 0.3 5.0 |> withColorAndMaterial (Some projectileColor) None
          makeSphereCmd (nextId "sphere") (1.0, 9.0, 0.0) 0.3 5.0 |> withColorAndMaterial (Some projectileColor) None ]
      runFor s 4.0
      // Act 3: Weld Cluster
      printfn "  Act 3: Weld Cluster"
      resetSimulation s
      setCamera s (4.0, 6.0, 6.0) (0.0, 2.0, 0.0) |> ignore
      batchAdd s
          [ for i in 0..9 do
              let x = float (i % 4) * 0.6 - 0.9
              let z = float (i / 4) * 0.6 - 0.6
              makeSphereCmd (nextId "sphere") (x, 0.3, z) 0.2 1.0
              |> withColorAndMaterial (Some targetColor) None ]
      runFor s 1.0
      let crossIds = [ for _ in 0..3 -> nextId "box" ]
      batchAdd s [
          makeBoxCmd crossIds.[0] (0.0, 6.0, 0.0) (0.6, 0.1, 0.1) 2.0 |> withColorAndMaterial (Some accentPurple) None
          makeBoxCmd crossIds.[1] (0.0, 6.0, 0.0) (0.1, 0.6, 0.1) 2.0 |> withColorAndMaterial (Some accentPurple) None
          makeBoxCmd crossIds.[2] (0.0, 6.0, 0.0) (0.1, 0.1, 0.6) 2.0 |> withColorAndMaterial (Some accentPurple) None
          makeBoxCmd crossIds.[3] (0.0, 6.5, 0.0) (0.2, 0.2, 0.2) 3.0 |> withColorAndMaterial (Some accentPurple) None ]
      batchAdd s [ for i in 1..3 do makeWeldInline (nextId "c") crossIds.[0] crossIds.[i] ]
      printfn "  Welded cross dropping onto pile..."
      runFor s 3.0
      printfn "  4 constraint types: ball-socket, distance-limit, hinge, weld"
      status s }

  // Demo 17: Query Range
  { Name = "Query Range"
    Description = "Raycasts, overlap tests, and sweep casts — physics queries in action."
    Run = fun s ->
      resetSimulation s
      setCamera s (8.0, 10.0, 8.0) (0.0, 2.0, 0.0) |> ignore
      setDemoInfo s "Demo 17: Query Range" "Raycasts, overlap tests, and sweep casts — physics queries in action."
      batchAdd s [
          makeBoxCmd (nextId "box") (0.0, 2.0, -2.1) (2.0, 2.0, 0.1) 0.0 |> withColorAndMaterial (Some structureColor) None
          makeBoxCmd (nextId "box") (0.0, 2.0, 2.1)  (2.0, 2.0, 0.1) 0.0 |> withColorAndMaterial (Some structureColor) None
          makeBoxCmd (nextId "box") (-2.1, 2.0, 0.0) (0.1, 2.0, 2.0) 0.0 |> withColorAndMaterial (Some structureColor) None
          makeBoxCmd (nextId "box") (2.1, 2.0, 0.0)  (0.1, 2.0, 2.0) 0.0 |> withColorAndMaterial (Some structureColor) None ]
      let rng = System.Random(88)
      batchAdd s
          [ for i in 0..19 do
              let x = rng.NextDouble() * 3.2 - 1.6
              let z = rng.NextDouble() * 3.2 - 1.6
              let y = 5.0 + rng.NextDouble() * 4.0
              let c = if i % 2 = 0 then accentYellow else accentGreen
              if i % 3 = 0 then
                  makeBoxCmd (nextId "box") (x, y, z) (0.15, 0.15, 0.15) 1.0
                  |> withColorAndMaterial (Some c) None
              else
                  makeSphereCmd (nextId "sphere") (x, y, z) 0.15 0.8
                  |> withColorAndMaterial (Some c) None ]
      printfn "  Dropped 20 bodies into pit — settling..."
      runFor s 3.0
      printfn "\n  === Raycast Tests ==="
      for xPos in [-2.0; -1.0; 0.0; 1.0; 2.0] do
          let hits = queryRaycast s (xPos, 10.0, 0.0) (0.0, -1.0, 0.0) 20.0
          match hits with
          | (bodyId, _, _, dist) :: _ -> printfn "  Raycast at X=%.0f: hit %s at distance %.2f" xPos bodyId dist
          | [] -> printfn "  Raycast at X=%.0f: no hit" xPos
      printfn "\n  === Overlap Test ==="
      let overlapping = queryOverlapSphere s 2.0 (0.0, 1.0, 0.0)
      printfn "  Overlap at center (r=2.0): %d bodies" overlapping.Length
      printfn "\n  === Sweep Test ==="
      match querySweepSphere s 0.3 (-3.0, 1.0, 0.0) (1.0, 0.0, 0.0) 6.0 with
      | Some (bodyId, _, _, dist) -> printfn "  Sweep: hit %s at distance %.2f" bodyId dist
      | None -> printfn "  Sweep: no hit"
      printfn ""
      status s }

  // Demo 18: Kinematic Sweep
  { Name = "Kinematic Sweep"
    Description = "A kinematic bulldozer plows through dynamic bodies — scripted path meets physics."
    Run = fun s ->
      resetSimulation s
      setCamera s (10.0, 6.0, 10.0) (0.0, 1.0, 0.0) |> ignore
      setDemoInfo s "Demo 18: Kinematic Sweep" "A kinematic bulldozer plows through dynamic bodies — scripted path meets physics."
      batchAdd s
          [ for i in 0..29 do
              let x = float (i % 6) * 0.8 - 2.0
              let z = float (i / 6) * 0.8 - 2.0
              makeSphereCmd (nextId "sphere") (x, 0.2, z) 0.15 0.5
              |> withColorAndMaterial (Some accentYellow) None ]
      runFor s 1.0
      let bulldozerId = "bulldozer"
      let boxShape = Shape()
      let b = Box()
      b.HalfExtents <- toVec3 (0.5, 0.5, 0.5)
      boxShape.Box <- b
      batchAdd s [ makeKinematicCmd bulldozerId (-4.0, 0.5, 0.0) boxShape
                   |> withColorAndMaterial (Some kinematicColor) None ]
      printfn "  30 dynamic spheres + 1 kinematic bulldozer"
      printfn "  Bulldozer advancing..."
      let speed = 2.0
      let totalDist = 8.0
      let totalTime = totalDist / speed
      let steps = 20
      let stepTime = totalTime / float steps
      setBodyPose s bulldozerId (-4.0, 0.5, 0.0) None (Some (speed, 0.0, 0.0)) None |> ok
      play s |> ignore
      for step in 0 .. steps - 1 do
          sleep (int (stepTime * 1000.0))
          if step % 5 = 0 then
              let x = -4.0 + float (step + 1) * (totalDist / float steps)
              printfn "  Step %d/%d — bulldozer at X=%.1f" (step+1) steps x
      pause s |> ignore
      printfn "  Sweep complete!"
      setCamera s (5.0, 4.0, 8.0) (0.0, 0.5, 0.0) |> ignore
      runFor s 2.0
      status s }

  // Demo 19: Shape Gallery
  { Name = "Shape Gallery"
    Description = "All shape types displayed side-by-side — the complete physics shape catalog."
    Run = fun s ->
      resetSimulation s
      setCamera s (8.0, 8.0, 15.0) (0.0, 2.0, 0.0) |> ignore
      setDemoInfo s "Demo 19: Shape Gallery" "All shape types displayed side-by-side — the complete physics shape catalog."
      let h = 6.0
      // Sphere
      batchAdd s [ makeSphereCmd (nextId "sphere") (-6.0, h, 0.0) 0.3 2.0
                   |> withColorAndMaterial (Some projectileColor) None ]
      // Box
      batchAdd s [ makeBoxCmd (nextId "box") (-4.0, h, 0.0) (0.25, 0.25, 0.25) 3.0
                   |> withColorAndMaterial (Some targetColor) None ]
      // Capsule
      batchAdd s [ makeCapsuleCmd (nextId "capsule") (-2.0, h, 0.0) 0.15 0.4 2.0
                   |> withColorAndMaterial (Some accentGreen) None ]
      // Cylinder
      batchAdd s [ makeCylinderCmd (nextId "cylinder") (0.0, h, 0.0) 0.2 0.5 3.0
                   |> withColorAndMaterial (Some accentOrange) None ]
      // Triangle
      batchAdd s [ makeTriangleCmd (nextId "triangle") (2.0, h, 0.0) (-0.3, -0.3, -0.3) (0.3, -0.3, -0.3) (0.0, 0.3, 0.3) 1.0
                   |> withColorAndMaterial (Some kinematicColor) None ]
      // ConvexHull (octahedron)
      let octaPts = [(0.4,0.0,0.0);(-0.4,0.0,0.0);(0.0,0.4,0.0);(0.0,-0.4,0.0);(0.0,0.0,0.4);(0.0,0.0,-0.4)]
      batchAdd s [ makeConvexHullCmd (nextId "hull") (4.0, h, 0.0) octaPts 2.5
                   |> withColorAndMaterial (Some accentPurple) None ]
      // Mesh (simple tetrahedron mesh)
      let meshTris = [
        ((-0.3, -0.2, -0.3), (0.3, -0.2, -0.3), (0.0, -0.2, 0.3))
        ((-0.3, -0.2, -0.3), (0.3, -0.2, -0.3), (0.0, 0.3, 0.0))
        ((0.3, -0.2, -0.3), (0.0, -0.2, 0.3), (0.0, 0.3, 0.0))
        ((-0.3, -0.2, -0.3), (0.0, -0.2, 0.3), (0.0, 0.3, 0.0)) ]
      batchAdd s [ makeMeshCmd (nextId "mesh") (6.0, h, 0.0) meshTris 2.0
                   |> withColorAndMaterial (Some accentYellow) None ]
      // Compound (dumbbell: two spheres offset)
      let s1 = Shape()
      let sp1 = Sphere()
      sp1.Radius <- 0.15
      s1.Sphere <- sp1
      let s2 = Shape()
      let sp2 = Sphere()
      sp2.Radius <- 0.15
      s2.Sphere <- sp2
      batchAdd s [ makeCompoundCmd (nextId "compound") (8.0, h, 0.0) [(s1, (-0.3, 0.0, 0.0)); (s2, (0.3, 0.0, 0.0))] 3.0
                   |> withColorAndMaterial (Some (makeColor 1.0 0.3 0.7 1.0)) None ]
      printfn "  All shape types dropping from %.0fm — watch the variety!" h
      runFor s 4.0
      setCamera s (4.0, 1.5, 8.0) (1.0, 0.3, 0.0) |> ignore
      printfn "  Ground-level view of the shape catalog"
      runFor s 2.0
      listBodies s }

  // Demo 20: Compound Constructions
  { Name = "Compound Constructions"
    Description = "Complex compound shapes — L-shapes, T-shapes, dumbbells — colliding and stacking."
    Run = fun s ->
      resetSimulation s
      setCamera s (6.0, 8.0, 10.0) (0.0, 2.0, 0.0) |> ignore
      setDemoInfo s "Demo 20: Compound Constructions" "Complex compound shapes — L-shapes, T-shapes, dumbbells — colliding and stacking."
      let h = 8.0
      // L-shapes (box + box at right angle)
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
      // T-shapes (horizontal bar + vertical stem)
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
      // Dumbbells (two spheres connected)
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
      printfn "  Closeup view of compound shape pile"
      runFor s 2.0
      listBodies s }

  // Demo 21: Mesh & Hull Playground
  { Name = "Mesh & Hull Playground"
    Description = "Convex hulls and triangle meshes of varied complexity tumbling through obstacles."
    Run = fun s ->
      resetSimulation s
      setCamera s (8.0, 8.0, 12.0) (0.0, 2.0, 0.0) |> ignore
      setDemoInfo s "Demo 21: Mesh & Hull Playground" "Convex hulls and triangle meshes of varied complexity tumbling through obstacles."
      // Static box obstacles
      for i in 0 .. 2 do
        let x = float i * 3.0 - 3.0
        batchAdd s [ makeBoxCmd (nextId "obstacle") (x, 0.5, 0.0) (0.3, 0.5, 2.0) 0.0
                     |> withColorAndMaterial (Some structureColor) None ]
      let h = 8.0
      // Tetrahedra (convex hulls with 4 points)
      for i in 0 .. 4 do
        let x = float i * 2.0 - 4.0
        let s_val = 0.2 + float i * 0.08
        let tetraPts = [(s_val, 0.0, 0.0); (-s_val, 0.0, 0.0); (0.0, s_val * 1.5, 0.0); (0.0, s_val * 0.5, s_val)]
        batchAdd s [ makeConvexHullCmd (nextId "tetra") (x, h, float i * 0.2 - 0.4) tetraPts (1.0 + float i * 0.5)
                     |> withColorAndMaterial (Some (makeColor 0.3 (0.5 + float i * 0.1) 1.0 1.0)) None ]
      // Octahedra (6 points)
      for i in 0 .. 3 do
        let x = float i * 2.5 - 3.5
        let r = 0.25 + float i * 0.05
        let octaPts = [(r,0.0,0.0);(-r,0.0,0.0);(0.0,r,0.0);(0.0,-r,0.0);(0.0,0.0,r);(0.0,0.0,-r)]
        batchAdd s [ makeConvexHullCmd (nextId "octa") (x, h + 2.0, 0.0) octaPts (2.0 + float i * 0.8)
                     |> withColorAndMaterial (Some accentPurple) None ]
      // Triangle meshes (simple pyramids)
      for i in 0 .. 3 do
        let x = float i * 2.5 - 3.0
        let s_val = 0.25 + float i * 0.05
        let meshTris = [
          ((-s_val, 0.0, -s_val), (s_val, 0.0, -s_val), (0.0, 0.0, s_val))
          ((-s_val, 0.0, -s_val), (s_val, 0.0, -s_val), (0.0, s_val * 1.5, 0.0))
          ((s_val, 0.0, -s_val), (0.0, 0.0, s_val), (0.0, s_val * 1.5, 0.0))
          ((-s_val, 0.0, -s_val), (0.0, 0.0, s_val), (0.0, s_val * 1.5, 0.0)) ]
        batchAdd s [ makeMeshCmd (nextId "mesh") (x, h + 4.0, float i * 0.3) meshTris (1.5 + float i * 0.4)
                     |> withColorAndMaterial (Some kinematicColor) None ]
      // Mixed triangle bodies
      for i in 0 .. 5 do
        let x = float i * 1.5 - 3.5
        batchAdd s [ makeTriangleCmd (nextId "tri") (x, h + 6.0, float i * 0.2 - 0.5) (-0.25, -0.2, -0.2) (0.25, -0.2, -0.2) (0.0, 0.25, 0.2) 0.8
                     |> withColorAndMaterial (Some (makeColor 1.0 (float i * 0.15) 0.3 1.0)) None ]
      printfn "  Dropping tetrahedra, octahedra, mesh pyramids, and triangles onto obstacles..."
      runFor s 5.0
      setCamera s (4.0, 2.0, 7.0) (0.0, 1.0, 0.0) |> ignore
      printfn "  Closeup of custom geometry shapes at rest"
      runFor s 2.0
      listBodies s }

  // Demo 22: Camera Showcase
  { Name = "Camera Showcase"
    Description = "Smooth camera transitions, body tracking, orbit, chase, framing, and shake."
    Run = fun s ->
      resetSimulation s
      setDemoInfo s "Demo 22: Camera Showcase" "Smooth camera transitions, body tracking, orbit, chase, framing, and shake."

      let ballId = nextId "sphere"
      batchAdd s [ makeSphereCmd ballId (0.0, 5.0, 0.0) 0.5 5.0
                   |> withColorAndMaterial (Some projectileColor) None ]
      let crateId = nextId "box"
      batchAdd s [ makeBoxCmd crateId (4.0, 3.0, 0.0) (0.4, 0.4, 0.4) 10.0
                   |> withColorAndMaterial (Some targetColor) None ]
      let capsuleId = nextId "capsule"
      batchAdd s [ makeCapsuleCmd capsuleId (-3.0, 4.0, 2.0) 0.3 0.8 4.0
                   |> withColorAndMaterial (Some accentGreen) None ]

      setNarration s "Smooth Camera — gliding to establishing shot"
      smoothCamera s (15.0, 10.0, 15.0) (0.0, 2.0, 0.0) 2.0
      sleep 2200

      setNarration s "Starting simulation"
      play s |> ignore
      sleep 2000

      setNarration s "Smooth Camera — zooming in"
      smoothCamera s (3.0, 2.0, 5.0) (0.0, 0.5, 0.0) 1.5
      sleep 1700

      setNarration s "LookAt — orienting toward the red ball"
      lookAtBody s ballId 1.5
      sleep 1700

      setNarration s "Follow — tracking the blue crate"
      followBody s crateId
      sleep 3000

      stopCamera s
      setNarration s "Pulling back for orbit"
      smoothCamera s (8.0, 6.0, 8.0) (0.0, 1.0, 0.0) 1.5
      sleep 1700

      setNarration s "Orbit — 360 degrees"
      let anchorId = nextId "box"
      batchAdd s [ makeBoxCmd anchorId (0.0, 0.5, 0.0) (0.1, 0.1, 0.1) 0.0 ]
      orbitBody s anchorId 5.0 360.0
      sleep 5200

      setNarration s "Chase — following green capsule"
      chaseBody s capsuleId (3.0, 3.0, 5.0)
      sleep 3000

      stopCamera s
      setNarration s "Frame Bodies — showing all objects"
      frameBodies s [ballId; crateId; capsuleId]
      sleep 3000

      setNarration s "Camera Shake — impact!"
      batchAdd s [ makeImpulseCmd ballId (0.0, 15.0, 0.0) ]
      shakeCamera s 0.3 1.0
      sleep 1200

      stopCamera s
      setNarration s "Final establishing shot"
      smoothCamera s (12.0, 8.0, 12.0) (0.0, 1.0, 0.0) 2.0
      sleep 2200

      pause s |> ignore
      clearNarration s
      printfn "  Camera showcase complete" }

  // ─── Demo 23: Ball Rollercoaster ──────────────────────────────────────
  { Name = "Ball Rollercoaster"
    Description = "Balls roll down a mesh terrain with drops, hills, and banked curves."
    Run = fun s ->
      let pi = System.Math.PI
      let terrainHeight (x: float) (z: float) =
          let t = (z + 15.0) / 30.0 |> max 0.0 |> min 1.0
          let baseY =
              if t < 0.1 then 8.0
              elif t < 0.25 then 8.0 - (t - 0.1) / 0.15 * 6.0
              elif t < 0.45 then 2.0 + 3.0 * sin((t - 0.25) / 0.20 * pi)
              elif t < 0.65 then 2.0 - (t - 0.45) * 3.0
              elif t < 0.85 then 0.8 + 2.0 * sin((t - 0.65) / 0.20 * pi)
              else 0.8 - (t - 0.85) / 0.15 * 0.3
          let edgeLift = 0.3 * (x * x) / 4.0
          let bank =
              if t > 0.35 && t < 0.55 then 0.3 * sin((t - 0.35) / 0.20 * pi) * x / 3.0
              else 0.0
          baseY + edgeLift + bank
      let xMin, xMax = -3.0, 3.0
      let zMin, zMax = -15.0, 15.0
      let xSteps = 4
      let zSteps = 15
      let dx = (xMax - xMin) / float xSteps
      let dz = (zMax - zMin) / float zSteps
      let mutable tris = []
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
              tris <- (p00, p10, p01) :: (p10, p11, p01) :: tris
      let trackTris = tris |> List.rev

      resetSimulation s
      setDemoInfo s "Demo 23: Ball Rollercoaster" "Balls roll down a mesh terrain with drops, hills, and banked curves."
      setNarration s "Building rollercoaster terrain..."
      smoothCamera s (8.0, 14.0, -10.0) (0.0, 4.0, 0.0) 1.5
      sleep 1700
      batchAdd s [ makeMeshCmd (nextId "track") (0.0, 0.0, 0.0) trackTris 0.0
                   |> withMotionType BodyMotionType.Static
                   |> withColorAndMaterial (Some accentYellow) (Some slipperyMaterial) ]
      sleep 500
      setNarration s "Releasing balls at the top!"
      smoothCamera s (4.0, 10.0, -16.0) (0.0, 8.0, -13.0) 1.5
      sleep 1700
      let ballColors = [| projectileColor; accentGreen; targetColor; accentOrange; accentPurple; kinematicColor |]
      let ballCmds =
          [ for i in 0 .. 5 ->
              makeSphereCmd (nextId "ball") (0.0, 9.0, -14.5 + float i * 0.6) 0.3 2.0
              |> withColorAndMaterial (Some ballColors.[i]) None ]
      batchAdd s ballCmds
      setNarration s "Steep drop — balls accelerating!"
      play s |> ignore
      smoothCamera s (5.0, 7.0, -8.0) (0.0, 3.0, -3.0) 2.0
      sleep 3500
      setNarration s "Over the hill!"
      smoothCamera s (-5.0, 7.0, 0.0) (0.0, 3.0, 3.0) 2.0
      sleep 3000
      setNarration s "Banked descent"
      smoothCamera s (-6.0, 5.0, 6.0) (0.0, 1.5, 9.0) 2.0
      sleep 3000
      setNarration s "Second hill and run-out"
      smoothCamera s (5.0, 5.0, 10.0) (0.0, 1.5, 14.0) 2.0
      sleep 3000
      setNarration s "Full terrain overview"
      smoothCamera s (12.0, 14.0, 0.0) (0.0, 3.0, 0.0) 2.0
      sleep 2500
      pause s |> ignore
      clearNarration s
      printfn "  Rollercoaster demo complete!" }

  // ─── Demo 24: Halfpipe Arena ──────────────────────────────────────────
  { Name = "Halfpipe Arena"
    Description = "Objects oscillate in a halfpipe bowl built from mesh triangles."
    Run = fun s ->
      let pipeHeight (x: float) (z: float) =
          let radius = 3.5
          let ax = abs x |> min radius
          let baseY = radius - sqrt(radius * radius - ax * ax)
          let zEdge = (abs z - 5.0) |> max 0.0
          let capLift = zEdge * zEdge * 0.15
          baseY + capLift
      let xMin, xMax = -4.0, 4.0
      let zMin, zMax = -8.0, 8.0
      let xSteps = 6
      let zSteps = 8
      let dx = (xMax - xMin) / float xSteps
      let dz = (zMax - zMin) / float zSteps
      let mutable tris = []
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
              tris <- (p00, p10, p01) :: (p10, p11, p01) :: tris
      let pipeTris = tris |> List.rev
      let halfpipeMat = makeMaterialProperties 0.3 4.0 30.0 0.8

      resetSimulation s
      setDemoInfo s "Demo 24: Halfpipe Arena" "Objects oscillate in a halfpipe bowl built from mesh triangles."
      setNarration s "Building halfpipe arena..."
      smoothCamera s (0.0, 12.0, 14.0) (0.0, 1.0, 0.0) 1.5
      sleep 1700
      batchAdd s [ makeMeshCmd (nextId "halfpipe") (0.0, 0.0, 0.0) pipeTris 0.0
                   |> withMotionType BodyMotionType.Static
                   |> withColorAndMaterial (Some accentYellow) (Some halfpipeMat) ]
      sleep 500
      setNarration s "Dropping balls into the halfpipe!"
      smoothCamera s (0.0, 6.0, -14.0) (0.0, 1.0, 0.0) 1.5
      sleep 1700
      let ballColors = [| projectileColor; accentGreen; targetColor; accentOrange; accentPurple; kinematicColor |]
      let ballCmds =
          [ for i in 0 .. 5 ->
              let x = (float i - 2.5) * 0.5
              let z = (float i - 2.5) * 1.5
              makeSphereCmd (nextId "ball") (x, 5.0, z) 0.35 2.5
              |> withColorAndMaterial (Some ballColors.[i]) None ]
      batchAdd s ballCmds
      let capCmds =
          [ for i in 0 .. 1 ->
              makeCapsuleCmd (nextId "capsule") (0.0, 6.0, float i * 3.0 - 1.5) 0.25 0.7 3.0
              |> withColorAndMaterial (Some accentPurple) None ]
      batchAdd s capCmds
      setNarration s "Objects falling into the bowl!"
      play s |> ignore
      smoothCamera s (0.0, 10.0, 12.0) (0.0, 1.0, 0.0) 2.0
      sleep 4000
      setNarration s "Oscillation — rolling back and forth"
      smoothCamera s (10.0, 5.0, 0.0) (0.0, 1.5, 0.0) 2.0
      sleep 4000
      setNarration s "Looking down the halfpipe"
      smoothCamera s (0.0, 5.0, -14.0) (0.0, 1.0, 0.0) 2.0
      sleep 3500
      setNarration s "Objects settling at the bottom"
      smoothCamera s (0.0, 8.0, 5.0) (0.0, 0.5, 0.0) 2.0
      sleep 3500
      setNarration s "Second wave incoming!"
      let wave2 =
          [ for i in 0 .. 2 ->
              makeSphereCmd (nextId "ball") (float i - 1.0, 6.0, float i * 2.0 - 2.0) 0.4 3.0
              |> withColorAndMaterial (Some accentGreen) None ]
      batchAdd s wave2
      sleep 3000
      setNarration s "Wide view — halfpipe arena"
      smoothCamera s (10.0, 10.0, 10.0) (0.0, 2.0, 0.0) 2.0
      sleep 2500
      pause s |> ignore
      clearNarration s
      printfn "  Halfpipe arena demo complete!" }

|]
