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

|]
