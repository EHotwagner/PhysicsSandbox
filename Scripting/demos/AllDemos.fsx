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

let demos = [|

  // Demo 01: Hello Drop
  { Name = "Hello Drop"
    Description = "Four different objects fall side by side — same gravity, different bounces."
    Run = fun s ->
      resetSimulation s
      setCamera s (6.0, 5.0, 8.0) (0.0, 3.0, 0.0) |> ignore
      let dropHeight = 10.0
      bowlingBall s (Some (-2.0, dropHeight, 0.0)) None None |> ignore
      let beachId = nextId "sphere"
      batchAdd s [ makeSphereCmd beachId (0.0, dropHeight, 0.0) 0.2 0.1 ]
      let crateId = nextId "box"
      batchAdd s [ makeBoxCmd crateId (2.0, dropHeight, 0.0) (0.25, 0.25, 0.25) 20.0 ]
      let dieId = nextId "box"
      batchAdd s [ makeBoxCmd dieId (3.5, dropHeight, 0.0) (0.05, 0.05, 0.05) 0.03 ]
      printfn "  Dropping: bowling ball, beach ball, crate, die — all from %.0fm" dropHeight
      printfn "  Same gravity, different shapes and masses..."
      runFor s 2.5
      setCamera s (4.0, 1.0, 5.0) (0.5, 0.2, 0.0) |> ignore
      printfn "  Ground-level view — notice different resting positions"
      runFor s 1.5
      let impulseCmds = [ for id in [beachId; crateId; dieId] do makeImpulseCmd id (0.0, 5.0, 0.0) ]
      batchAdd s impulseCmds
      printfn "  Upward impulse applied — watch the light ones fly!"
      setCamera s (6.0, 5.0, 8.0) (0.0, 3.0, 0.0) |> ignore
      runFor s 3.0
      listBodies s }

  // Demo 02: Bouncing Marbles
  { Name = "Bouncing Marbles"
    Description = "Two waves of marbles in varied sizes — bouncing, colliding, settling."
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
              makeSphereCmd (nextId "sphere") (x, y, z) radius mass ]
      batchAdd s wave1
      printfn "  Wave 1: 15 marbles raining down..."
      runFor s 3.0
      setCamera s (3.0, 4.0, 3.0) (0.0, 0.5, 0.0) |> ignore
      let wave2 =
          [ for i in 0 .. 9 do
              let x = rng.NextDouble() * 1.6 - 0.8
              let z = rng.NextDouble() * 1.6 - 0.8
              let y = 8.0 + rng.NextDouble() * 3.0
              let radius = 0.08 + rng.NextDouble() * 0.12
              let mass = radius * radius * 10.0
              makeSphereCmd (nextId "sphere") (x, y, z) radius mass ]
      batchAdd s wave2
      printfn "  Wave 2: 10 more marbles into the pile!"
      runFor s 3.0
      printfn "  Settled."
      sleep 1000
      listBodies s }

  // Demo 03: Crate Stack
  { Name = "Crate Stack"
    Description = "A 12-crate tower hit by a bowling ball — dramatic toppling."
    Run = fun s ->
      resetSimulation s
      setCamera s (8.0, 7.0, 4.0) (0.0, 5.0, 0.0) |> ignore
      let ids = stack s 12 (Some (0.0, 0.0, 0.0)) |> ok
      printfn "  Built tower of %d crates" ids.Length
      runFor s 2.0
      let ball = bowlingBall s (Some (-5.0, 3.0, 0.0)) None None |> ok
      printfn "  Bowling ball ready — aiming at tower base..."
      sleep 500
      printfn "  STRIKE!"
      launch s ball (0.0, 3.0, 0.0) 30.0 |> ignore
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
    Description = "Launch a bowling ball at a pyramid of bricks."
    Run = fun s ->
      resetSimulation s
      setCamera s (-5.0, 3.0, 3.0) (3.0, 1.0, 0.0) |> ignore
      pyramid s 4 (Some (5.0, 0.0, 0.0)) |> ok |> ignore
      printfn "  Built pyramid (4 layers)"
      let ball = bowlingBall s (Some (-3.0, 0.15, 0.0)) None None |> ok
      printfn "  Bowling ball ready"
      runFor s 1.0
      printfn "  Admiring the pyramid..."
      sleep 1500
      printfn "  STRIKE! Launching ball..."
      launch s ball (5.0, 0.5, 0.0) 25.0 |> ignore
      runFor s 2.5
      setCamera s (2.0, 0.5, 3.0) (5.0, 0.5, 0.0) |> ignore
      printfn "  Low-angle debris view"
      runFor s 2.0
      listBodies s }

  // Demo 05: Marble Rain
  { Name = "Marble Rain"
    Description = "40 mixed objects rain from the sky — spheres, crates, and dice piling up."
    Run = fun s ->
      resetSimulation s
      setCamera s (6.0, 10.0, 6.0) (0.0, 0.0, 0.0) |> ignore
      let ids = randomSpheres s 20 (Some 42) |> ok
      printfn "  Wave 1: %d random spheres raining down..." ids.Length
      runFor s 3.0
      let rng = System.Random(99)
      let wave2 =
          [ for i in 0 .. 9 do
                let x = rng.NextDouble() * 4.0 - 2.0
                let z = rng.NextDouble() * 4.0 - 2.0
                let y = 8.0 + rng.NextDouble() * 5.0
                let half = 0.1 + rng.NextDouble() * 0.15
                makeBoxCmd (nextId "box") (x, y, z) (half, half, half) (half * 80.0)
            for i in 0 .. 9 do
                let x = rng.NextDouble() * 3.0 - 1.5
                let z = rng.NextDouble() * 3.0 - 1.5
                let y = 10.0 + rng.NextDouble() * 4.0
                makeBoxCmd (nextId "box") (x, y, z) (0.05, 0.05, 0.05) 0.03 ]
      batchAdd s wave2
      printfn "  Wave 2: 10 crates + 10 dice joining the pile!"
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
              makeBoxCmd ids.[i] (x, 0.3, 0.0) (0.05, 0.3, 0.15) 1.0 ]
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
              if i % 2 = 0 then makeSphereCmd ids.[i] (x, 0.3, z) 0.25 2.0
              else makeBoxCmd ids.[i] (x, 0.4, z) (0.3, 0.3, 0.3) 5.0 ]
      batchAdd s bodyCmds
      printfn "  6 objects in a ring"
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
      let bodyCmds =
          [ for i in 0 .. 7 do
                let x = float (i % 4) * 1.2 - 1.8
                let z = float (i / 4) * 1.2 - 0.6
                let y = 3.0 + rng.NextDouble() * 4.0
                makeSphereCmd (nextId "sphere") (x, y, z) 0.2 0.1
            for i in 0 .. 11 do
                let x = float (i % 4) * 0.8 - 1.2
                let z = float (i / 4) * 0.8 - 1.0
                let y = 5.0 + rng.NextDouble() * 3.0
                makeBoxCmd (nextId "box") (x, y, z) (0.05, 0.05, 0.05) 0.03
            for i in 0 .. 4 do
                let x = float i * 1.0 - 2.0
                makeSphereCmd (nextId "sphere") (x, 1.5, 0.0) 0.15 1.0 ]
      batchAdd s bodyCmds
      printfn "  25 objects: beach balls, dice, spheres"
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
      let cmds = [
          for row in 0..4 do
            for col in 0..row do
              let x = float row * spacing * 0.866 + 1.0
              let z = (float col - float row / 2.0) * spacing
              makeSphereCmd (nextId "sphere") (x, r, z) r 0.17
          makeSphereCmd cueId (-2.0, r, 0.0) (r * 1.1) 0.17 ]
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
    Description = "Everything: presets, generators, steering, gravity, camera sweeps."
    Run = fun s ->
      resetSimulation s
      setCamera s (12.0, 8.0, 12.0) (0.0, 2.0, 0.0) |> ignore
      printfn "  Act 1: Building the stage..."
      pyramid s 5 (Some (-4.0, 0.0, 0.0)) |> ok |> ignore
      stack s 6 (Some (4.0, 0.0, 0.0)) |> ok |> ignore
      row s 8 (Some (-3.0, 0.0, 3.0)) |> ok |> ignore
      runFor s 1.5
      sleep 800
      printfn "  Act 2: Bombardment!"
      setCamera s (0.0, 15.0, 10.0) (0.0, 2.0, 0.0) |> ignore
      let projectiles = randomSpheres s 10 (Some 99) |> ok
      let impulseCmds =
          [ for id in projectiles do makeImpulseCmd id (0.0, -20.0, 0.0) ]
      batchAdd s impulseCmds
      runFor s 3.0
      printfn "  Act 3: Boulder attack!"
      setCamera s (-10.0, 3.0, 0.0) (0.0, 2.0, 0.0) |> ignore
      let rock = boulder s (Some (-8.0, 1.0, 0.0)) None None |> ok
      launch s rock (-4.0, 2.0, 0.0) 30.0 |> ignore
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
                    if i % 3 = 0 then
                        makeBoxCmd (nextId "box") (x, y, z) (0.2, 0.2, 0.2) (0.5 + rng.NextDouble() * 2.0)
                    else
                        makeSphereCmd (nextId "sphere") (x, y, z) (0.15 + rng.NextDouble() * 0.1) (0.5 + rng.NextDouble() * 2.0) ]
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
      timed "Wave 1 — 40 large spheres" (fun () ->
          let wave1 =
              [ for i in 0 .. 39 do
                  let x = float (i % 8) * 0.45 - 1.6
                  let z = float (i / 8) * 0.45 - 0.9
                  let y = 6.0 + rng.NextDouble() * 2.0
                  makeSphereCmd (nextId "sphere") (x, y, z) (0.15 + rng.NextDouble() * 0.1) (0.3 + rng.NextDouble() * 1.0) ]
          batchAdd s wave1)
      printfn "  Wave 1: 40 large spheres dropping..."
      runFor s 3.0
      setCamera s (5.0, 8.0, 5.0) (0.0, 3.0, 0.0) |> ignore
      timed "Wave 2 — 60 small marbles" (fun () ->
          let wave2 =
              [ for i in 0 .. 59 do
                  let x = rng.NextDouble() * 3.2 - 1.6
                  let z = rng.NextDouble() * 3.2 - 1.6
                  let y = 10.0 + rng.NextDouble() * 4.0
                  makeSphereCmd (nextId "sphere") (x, y, z) (0.06 + rng.NextDouble() * 0.06) (0.1 + rng.NextDouble() * 0.3) ]
          batchAdd s wave2)
      printfn "  Wave 2: 60 small marbles into the pile!"
      runFor s 4.0
      timed "Wave 3 — 20 heavy spheres" (fun () ->
          let wave3 =
              [ for i in 0 .. 19 do
                  let x = rng.NextDouble() * 2.4 - 1.2
                  let z = rng.NextDouble() * 2.4 - 1.2
                  let y = 12.0 + rng.NextDouble() * 3.0
                  makeSphereCmd (nextId "sphere") (x, y, z) 0.2 3.0 ]
          batchAdd s wave3)
      printfn "  Wave 3: 20 heavy spheres — IMPACT!"
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
                      makeSphereCmd bodyIds.[idx] (x, 0.5, z) radius mass ]
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
                      // Domino orientation: thin along the radial direction
                      makeBoxCmd dominoIds.[i] (x, 0.3, z) (0.05, 0.3, 0.15) 1.0 ]
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
      // Act 2: Sphere rain
      let sphereIds =
          timed "Act 2 — 100 random spheres" (fun () ->
              let cmds =
                  [ for i in 0 .. 99 do
                      let x = float (i % 10) * 1.5 - 7.0
                      let z = float (i / 10) * 1.5 - 7.0
                      let y = 8.0 + float (i / 20) * 2.0
                      makeSphereCmd (nextId "sphere") (x, y, z) 0.25 0.8 ]
              batchAdd s cmds
              let ids = [ for i in 0 .. 99 -> sprintf "sphere-%d" (i + 1) ]
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

|]
