// AllDemos.fsx — All 15 demos defined as inline functions
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
      printfn "  Dropping: ball, beach ball, crate, die, capsule, cylinder — all from %.0fm" dropHeight
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
      // Build tower of colored crates
      let towerCmds =
          [ for i in 0..11 do
              let y = 0.5 + float i * 1.0
              makeBoxCmd (nextId "box") (0.0, y, 0.0) (0.5, 0.5, 0.5) 2.0
              |> withColorAndMaterial (Some targetColor) None ]
      batchAdd s towerCmds
      printfn "  Built tower of 12 crates"
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
      printfn "  Built pyramid (4 layers) at Z=5"
      let ballId = nextId "sphere"
      batchAdd s [ makeSphereCmd ballId (0.0, 0.5, -2.0) 0.4 10.0
                   |> withColorAndMaterial (Some projectileColor) None ]
      printfn "  Bowling ball at Z=-2, aimed head-on"
      runFor s 1.0
      printfn "  Admiring the pyramid..."
      sleep 1500
      printfn "  STRIKE! Launching ball..."
      launch s ballId (0.0, 1.0, 5.0) 40.0 |> ignore
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
      let ids = [ for _ in 0..19 -> nextId "box" ]
      let cmds =
          [ for i in 0..19 do
              let x = float i * 0.5
              let t = float i / 19.0
              let c = makeColor (0.3 + t * 0.5) (0.6 - t * 0.2) (1.0 - t * 0.6) 1.0
              makeBoxCmd ids.[i] (x, 0.3, 0.0) (0.05, 0.3, 0.15) 1.0
              |> withColorAndMaterial (Some c) None ]
      batchAdd s cmds
      let firstId = ids.[0]
      printfn "  Placed 20 dominoes"
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
      printfn "  15 balls + cue placed"
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
      printfn "  80 spheres in tight 8x10 grid"
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
                      makeBoxCmd dominoIds.[i] (x, 0.3, z) (0.05, 0.3, 0.15) 1.0
                      |> withColorAndMaterial (Some c) None ]
              batchAdd s cmds
              dominoIds)
      printfn "  %d dominoes in semicircle (radius %.0fm)" count radius
      runFor s 1.0
      // Brief overhead view to show the full semicircle layout
      setCamera s (0.0, 14.0, 0.1) (0.0, 0.0, 0.0) |> ignore
      printfn "  Overhead view — full semicircle"
      sleep 1000
      // Move to side view for the push
      setCamera s (radius + 2.0, 3.0, 0.0) (0.0, 0.5, 0.0) |> ignore
      printfn "  Pushing first domino..."
      push s ids.[0] East 4.0 |> ignore
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

|]
