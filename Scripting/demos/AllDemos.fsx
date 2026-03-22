// AllDemos.fsx — All 10 demos defined as inline functions
// Loaded by RunAll.fsx and AutoRun.fsx

#load "Prelude.fsx"

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
    Description = "A single bowling ball falls from height onto the ground."
    Run = fun s ->
      resetSimulation s
      setCamera s (5.0, 3.0, 5.0) (0.0, 1.0, 0.0) |> ignore
      bowlingBall s (Some (0.0, 10.0, 0.0)) None None |> ignore
      printfn "  Dropping bowling ball from 10m..."
      runFor s 3.0
      listBodies s }

  // Demo 02: Bouncing Marbles
  { Name = "Bouncing Marbles"
    Description = "Five marbles dropped from different heights."
    Run = fun s ->
      resetSimulation s
      setCamera s (4.0, 5.0, 4.0) (0.0, 0.5, 0.0) |> ignore
      let cmds =
          [ for i in 0..4 do
              let x = float i * 0.3 - 0.6
              let y = 3.0 + float i * 2.0
              makeSphereCmd (nextId "sphere") (x, y, 0.0) 0.01 0.005 ]
      batchAdd s cmds
      printfn "  Dropping 5 marbles from 3m to 11m..."
      runFor s 4.0
      listBodies s }

  // Demo 03: Crate Stack
  { Name = "Crate Stack"
    Description = "A tower of 8 crates — push the top one off."
    Run = fun s ->
      resetSimulation s
      setCamera s (6.0, 5.0, 0.0) (0.0, 4.0, 0.0) |> ignore
      let ids = stack s 8 (Some (0.0, 0.0, 0.0)) |> ok
      printfn "  Built stack of %d crates" ids.Length
      runFor s 2.0
      let topId = ids |> List.last
      printfn "  Pushing top crate east..."
      push s topId East 15.0 |> ignore
      runFor s 3.0
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
      printfn "  STRIKE! Launching ball..."
      launch s ball (5.0, 0.5, 0.0) 25.0 |> ignore
      runFor s 4.0
      listBodies s }

  // Demo 05: Marble Rain
  { Name = "Marble Rain"
    Description = "20 random spheres rain down from the sky."
    Run = fun s ->
      resetSimulation s
      setCamera s (8.0, 12.0, 8.0) (0.0, 0.0, 0.0) |> ignore
      let ids = randomSpheres s 20 (Some 42) |> ok
      printfn "  Generated %d random spheres" ids.Length
      printfn "  Let it rain..."
      runFor s 5.0
      setCamera s (3.0, 1.0, 3.0) (0.0, 0.5, 0.0) |> ignore
      printfn "  Ground-level view"
      sleep 1000
      listBodies s }

  // Demo 06: Domino Row
  { Name = "Domino Row"
    Description = "A row of 12 brick dominoes toppled by a push."
    Run = fun s ->
      resetSimulation s
      setCamera s (-2.0, 3.0, 6.0) (3.0, 0.5, 0.0) |> ignore
      let ids = [ for _ in 0..11 -> nextId "box" ]
      let cmds =
          [ for i in 0..11 do
              let x = float i * 0.5
              makeBoxCmd ids.[i] (x, 0.3, 0.0) (0.05, 0.3, 0.15) 1.0 ]
      batchAdd s cmds
      let firstId = ids.[0]
      printfn "  Placed 12 dominoes"
      runFor s 1.0
      printfn "  Toppling first domino..."
      push s firstId East 3.0 |> ignore
      runFor s 5.0
      setCamera s (8.0, 2.0, 4.0) (5.0, 0.0, 0.0) |> ignore
      sleep 500
      listBodies s }

  // Demo 07: Spinning Tops
  { Name = "Spinning Tops"
    Description = "Beach balls and crates spinning with applied torques."
    Run = fun s ->
      resetSimulation s
      setCamera s (0.0, 8.0, 6.0) (0.0, 0.5, 0.0) |> ignore
      let b1id = nextId "sphere"
      let b2id = nextId "sphere"
      let b3id = nextId "box"
      let b4id = nextId "box"
      let bodyCmds = [
          makeSphereCmd b1id (-2.0, 0.25, 0.0) 0.2 0.1
          makeSphereCmd b2id (2.0, 0.25, 0.0) 0.2 0.1
          makeBoxCmd b3id (0.0, 0.55, -2.0) (0.5, 0.5, 0.5) 20.0
          makeBoxCmd b4id (0.0, 0.55, 2.0) (0.5, 0.5, 0.5) 20.0 ]
      batchAdd s bodyCmds
      printfn "  4 bodies placed"
      runFor s 0.5
      let torqueCmds = [
          makeTorqueCmd b1id (0.0, 50.0, 0.0)
          makeTorqueCmd b2id (0.0, 0.0, -30.0)
          makeTorqueCmd b3id (0.0, 80.0, 0.0)
          makeTorqueCmd b4id (40.0, 0.0, 0.0) ]
      batchAdd s torqueCmds
      printfn "  Spinning..."
      runFor s 4.0
      wireframe s true |> ignore
      printfn "  Wireframe on"
      sleep 2000
      wireframe s false |> ignore
      listBodies s }

  // Demo 08: Gravity Flip
  { Name = "Gravity Flip"
    Description = "Objects settle, then gravity flips upward!"
    Run = fun s ->
      resetSimulation s
      setCamera s (6.0, 4.0, 6.0) (0.0, 2.0, 0.0) |> ignore
      grid s 3 3 (Some (-2.0, 0.0, -2.0)) |> ok |> ignore
      let ballCmds =
          [ for i in 0..4 do
              let x = float i * 0.8 - 1.6
              makeSphereCmd (nextId "sphere") (x, 5.0 + float i, 0.3) 0.2 0.1 ]
      batchAdd s ballCmds
      printfn "  Grid + 5 beach balls"
      runFor s 3.0
      printfn "  Settled. GRAVITY REVERSED!"
      setCamera s (5.0, 1.0, 5.0) (0.0, 5.0, 0.0) |> ignore
      setGravity s (0.0, 15.0, 0.0) |> ignore
      runFor s 3.0
      printfn "  Sideways gravity..."
      setGravity s (10.0, 0.0, 0.0) |> ignore
      setCamera s (-8.0, 3.0, 0.0) (0.0, 2.0, 0.0) |> ignore
      runFor s 2.0
      setGravity s (0.0, -9.81, 0.0) |> ignore
      printfn "  Gravity restored"
      runFor s 2.0
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
      setCamera s (-3.0, 1.5, 2.0) (1.0, 0.0, 0.0) |> ignore
      runFor s 0.5
      printfn "  BREAK!"
      launch s cueId (1.5, 0.0, 0.0) 15.0 |> ignore
      runFor s 4.0
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
      for angle in 0..8 do
        let a = float angle * 0.7
        setCamera s (10.0 * cos a, 5.0, 10.0 * sin a) (0.0, 1.0, 0.0) |> ignore
        sleep 400
      printfn "  Chaos complete!"
      status s }

  // Demo 11: Body Scaling
  { Name = "Body Scaling"
    Description = "Progressive body count: 50 → 100 → 200 → 500. Finds degradation point."
    Run = fun s ->
      resetSimulation s
      let tiers = [50; 100; 200; 500]
      for tier in tiers do
        printfn "  === Tier: %d bodies ===" tier
        resetSimulation s
        let dist = if tier <= 100 then 15.0 elif tier <= 200 then 25.0 else 40.0
        setCamera s (dist, dist * 0.6, dist) (0.0, 2.0, 0.0) |> ignore
        timed (sprintf "Tier %d setup" tier) (fun () ->
            let cmds =
                [ for i in 0 .. tier - 1 do
                    let x = float (i % 10) * 1.2 - 6.0
                    let y = 2.0 + float (i / 100) * 3.0
                    let z = float ((i / 10) % 10) * 1.2 - 6.0
                    makeSphereCmd (nextId "sphere") (x, y, z) 0.3 1.0 ]
            batchAdd s cmds)
        timed (sprintf "Tier %d simulation (3s)" tier) (fun () ->
            runFor s 3.0)
        printfn "  Tier %d complete" tier
      printfn "  All tiers complete — check [TIME] markers for degradation"
      status s }

  // Demo 12: Collision Pit
  { Name = "Collision Pit"
    Description = "120 spheres dropped into a walled pit — maximum collision density."
    Run = fun s ->
      resetSimulation s
      setCamera s (8.0, 10.0, 8.0) (0.0, 2.0, 0.0) |> ignore
      timed "Pit walls setup" (fun () ->
          // 4 walls forming a 4x4m pit (static boxes, mass=0)
          let wallCmds = [
              makeBoxCmd (nextId "box") (0.0, 2.0, -2.1) (2.0, 2.0, 0.1) 0.0  // back wall
              makeBoxCmd (nextId "box") (0.0, 2.0, 2.1)  (2.0, 2.0, 0.1) 0.0  // front wall
              makeBoxCmd (nextId "box") (-2.1, 2.0, 0.0) (0.1, 2.0, 2.0) 0.0  // left wall
              makeBoxCmd (nextId "box") (2.1, 2.0, 0.0)  (0.1, 2.0, 2.0) 0.0  // right wall
          ]
          batchAdd s wallCmds)
      printfn "  Pit built (4x4m walled enclosure)"
      timed "Drop 120 spheres" (fun () ->
          let sphereCmds =
              [ for i in 0 .. 119 do
                  let x = float (i % 10) * 0.35 - 1.6
                  let z = float ((i / 10) % 12) * 0.35 - 1.9
                  let y = 6.0 + float (i / 10) * 0.5
                  makeSphereCmd (nextId "sphere") (x, y, z) 0.15 0.5 ]
          batchAdd s sphereCmds)
      printfn "  120 spheres dropping into pit..."
      timed "Settling simulation (8s)" (fun () ->
          runFor s 8.0)
      setCamera s (4.0, 3.0, 4.0) (0.0, 1.5, 0.0) |> ignore
      printfn "  Close-up view of settled pit"
      sleep 1000
      status s }

  // Demo 13: Force Frenzy
  { Name = "Force Frenzy"
    Description = "100 bodies hit with 3 rounds of escalating impulses, torques, and gravity shifts."
    Run = fun s ->
      resetSimulation s
      setCamera s (15.0, 10.0, 15.0) (0.0, 1.0, 0.0) |> ignore
      let ids =
          timed "Create 100 bodies" (fun () ->
              let bodyIds = [ for i in 0 .. 99 -> nextId "sphere" ]
              let cmds =
                  [ for idx in 0 .. 99 do
                      let x = float (idx % 10) * 1.5 - 7.0
                      let z = float (idx / 10) * 1.5 - 7.0
                      makeSphereCmd bodyIds.[idx] (x, 0.5, z) 0.3 1.0 ]
              batchAdd s cmds
              bodyIds)
      printfn "  100 spheres in 10x10 grid"
      timed "Settle (2s)" (fun () -> runFor s 2.0)
      // Round 1: impulses
      timed "Round 1 — impulses (3s)" (fun () ->
          let impCmds = [ for id in ids do makeImpulseCmd id (0.0, 8.0, 0.0) ]
          batchAdd s impCmds
          runFor s 3.0)
      // Round 2: torques + sideways gravity
      timed "Round 2 — torques + sideways gravity (3s)" (fun () ->
          let torCmds = [ for id in ids do makeTorqueCmd id (0.0, 20.0, 10.0) ]
          batchAdd s torCmds
          setGravity s (8.0, -2.0, 0.0) |> ignore
          setCamera s (-15.0, 5.0, 10.0) (0.0, 2.0, 0.0) |> ignore
          runFor s 3.0)
      // Round 3: strong impulses + reversed gravity
      timed "Round 3 — strong impulses + reversed gravity (3s)" (fun () ->
          let impCmds2 = [ for id in ids do makeImpulseCmd id (5.0, 15.0, -5.0) ]
          batchAdd s impCmds2
          setGravity s (0.0, 12.0, 0.0) |> ignore
          setCamera s (10.0, 2.0, 15.0) (0.0, 5.0, 0.0) |> ignore
          runFor s 3.0)
      setGravity s (0.0, -9.81, 0.0) |> ignore
      printfn "  Gravity restored"
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
      printfn "  Pushing first domino..."
      // Push first domino inward (toward center)
      push s ids.[0] East 4.0 |> ignore
      timed "Cascade propagation" (fun () ->
          runFor s 10.0)
      // Camera sweep along the cascade
      for i in 0..5 do
          let angle = float i / 5.0 * System.Math.PI
          let cx = (radius + 4.0) * cos angle
          let cz = (radius + 4.0) * sin angle
          setCamera s (cx, 3.0, cz) (0.0, 0.5, 0.0) |> ignore
          sleep 500
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
      // Act 3: Impulse storm
      timed "Act 3 — impulse storm" (fun () ->
          setCamera s (0.0, 20.0, 15.0) (0.0, 2.0, 0.0) |> ignore
          // Apply impulses to all pyramid bodies
          let impCmds =
              [ for id in pyramidIds do makeImpulseCmd id (0.0, 10.0, 3.0) ]
          batchAdd s impCmds
          runFor s 3.0)
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
