// 16_Constraints.fsx — Pendulum chain, hinged bridge, weld cluster
// Usage: dotnet fsi Scripting/demos/16_Constraints.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsSandbox.Shared.Contracts

// ─── Inline constraint helpers for types not in Prelude ─────────────

let makeDistanceLimitCmd (id: string) (bodyA: string) (bodyB: string)
                         (offsetA: float * float * float) (offsetB: float * float * float)
                         (minDist: float) (maxDist: float) =
    let dl = DistanceLimitConstraint()
    dl.LocalOffsetA <- toVec3 offsetA
    dl.LocalOffsetB <- toVec3 offsetB
    dl.MinDistance <- minDist
    dl.MaxDistance <- maxDist
    let spring = SpringSettings()
    spring.Frequency <- 30.0
    spring.DampingRatio <- 1.0
    dl.Spring <- spring
    let ct = ConstraintType()
    ct.DistanceLimit <- dl
    let ac = AddConstraint()
    ac.Id <- id; ac.BodyA <- bodyA; ac.BodyB <- bodyB; ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

let makeWeldCmd (id: string) (bodyA: string) (bodyB: string) =
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
    ac.Id <- id; ac.BodyA <- bodyA; ac.BodyB <- bodyB; ac.Type <- ct
    let cmd = SimulationCommand()
    cmd.AddConstraint <- ac
    cmd

// ─── Demo ───────────────────────────────────────────────────────────

let run (s: Session) =
    resetSimulation s

    // ── Act 1: Pendulum Chain ──────────────────────────────────────
    // 5 spheres linked by ball-socket + distance-limit constraints
    setNarration s "Act 1: Building a pendulum chain"
    smoothCamera s (0.0, 8.0, 10.0) (0.0, 5.0, 0.0) 1.5
    sleep 1700
    printfn "  Act 1: Pendulum Chain"
    setDemoInfo s "Demo 16: Constraints" "Pendulum chains, hinged bridges, and welded structures."

    // Static anchor at the top
    let anchorId = nextId "box"
    batchAdd s [ makeBoxCmd anchorId (0.0, 8.0, 0.0) (0.3, 0.1, 0.1) 0.0
                 |> withColorAndMaterial (Some structureColor) None ]

    // 5 pendulum spheres hanging below
    let spacing = 0.8
    let pendIds = [ for i in 0..4 -> nextId "sphere" ]
    let pendCmds =
        [ for i in 0..4 do
            let y = 7.0 - float i * spacing
            let cmd = makeSphereCmd pendIds.[i] (0.0, y, 0.0) 0.2 1.0
            if i = 4 then
                cmd |> withColorAndMaterial (Some accentYellow) (Some bouncyMaterial)
            else
                cmd |> withColorAndMaterial (Some accentYellow) None ]
    batchAdd s pendCmds

    // Ball-socket constraints to link chain
    let socketCmds = [
        makeBallSocketCmd (nextId "constraint") anchorId pendIds.[0] (0.0, -0.1, 0.0) (0.0, 0.2, 0.0)
        for i in 0..3 do
            makeBallSocketCmd (nextId "constraint") pendIds.[i] pendIds.[i+1] (0.0, -0.3, 0.0) (0.0, 0.3, 0.0) ]
    batchAdd s socketCmds

    // Distance-limit constraints to keep links from stretching
    let distCmds = [
        makeDistanceLimitCmd (nextId "constraint") anchorId pendIds.[0] (0.0, -0.1, 0.0) (0.0, 0.2, 0.0) 0.0 0.7
        for i in 0..3 do
            makeDistanceLimitCmd (nextId "constraint") pendIds.[i] pendIds.[i+1] (0.0, -0.3, 0.0) (0.0, 0.3, 0.0) 0.0 0.7 ]
    batchAdd s distCmds

    runFor s 1.0
    // Disturb the first sphere
    setNarration s "Disturbing the pendulum — watch it swing!"
    printfn "  Disturbing pendulum..."
    batchAdd s [ makeImpulseCmd pendIds.[0] (5.0, 0.0, 0.0) ]
    runFor s 4.0

    // ── Act 2: Hinged Bridge ───────────────────────────────────────
    // 6 planks linked by hinge constraints between two pillars
    setNarration s "Act 2: Constructing a hinged bridge"
    printfn "\n  Act 2: Hinged Bridge"
    resetSimulation s
    smoothCamera s (0.0, 5.0, 8.0) (0.0, 3.0, 0.0) 1.5
    sleep 1700

    // Two static pillars
    let pillarL = nextId "box"
    let pillarR = nextId "box"
    batchAdd s [
        makeBoxCmd pillarL (-3.5, 2.0, 0.0) (0.3, 2.0, 0.3) 0.0 |> withColorAndMaterial (Some structureColor) None
        makeBoxCmd pillarR (3.5, 2.0, 0.0) (0.3, 2.0, 0.3) 0.0 |> withColorAndMaterial (Some structureColor) None ]

    // 6 bridge planks
    let plankIds = [ for i in 0..5 -> nextId "box" ]
    let plankCmds =
        [ for i in 0..5 do
            let x = -2.5 + float i * 1.0
            makeBoxCmd plankIds.[i] (x, 4.0, 0.0) (0.5, 0.05, 0.3) 2.0
            |> withColorAndMaterial (Some accentOrange) None ]
    batchAdd s plankCmds

    // Hinge constraints between planks and anchored to pillars
    let hingeCmds = [
        makeHingeCmd (nextId "constraint") pillarL plankIds.[0] (0.0, 0.0, 1.0) (0.3, 2.0, 0.0) (-0.5, 0.0, 0.0)
        for i in 0..4 do
            makeHingeCmd (nextId "constraint") plankIds.[i] plankIds.[i+1] (0.0, 0.0, 1.0) (0.5, 0.0, 0.0) (-0.5, 0.0, 0.0)
        makeHingeCmd (nextId "constraint") plankIds.[5] pillarR (0.0, 0.0, 1.0) (0.5, 0.0, 0.0) (-0.3, 2.0, 0.0) ]
    batchAdd s hingeCmds

    runFor s 1.0
    // Drop heavy spheres on the bridge
    setNarration s "Dropping heavy weights onto the bridge"
    printfn "  Dropping weights on bridge..."
    batchAdd s [
        makeSphereCmd (nextId "sphere") (-1.0, 8.0, 0.0) 0.3 5.0 |> withColorAndMaterial (Some projectileColor) None
        makeSphereCmd (nextId "sphere") (1.0, 9.0, 0.0) 0.3 5.0 |> withColorAndMaterial (Some projectileColor) None ]
    runFor s 4.0

    // ── Act 3: Weld Cluster ────────────────────────────────────────
    // 4 boxes welded into a cross, dropped onto a pile
    setNarration s "Act 3: Welded cross dropping onto a pile"
    printfn "\n  Act 3: Weld Cluster"
    resetSimulation s
    smoothCamera s (4.0, 6.0, 6.0) (0.0, 2.0, 0.0) 1.5
    sleep 1700

    // Create a pile of spheres to land on
    let pileCmds =
        [ for i in 0..9 do
            let x = float (i % 4) * 0.6 - 0.9
            let z = float (i / 4) * 0.6 - 0.6
            makeSphereCmd (nextId "sphere") (x, 0.3, z) 0.2 1.0
            |> withColorAndMaterial (Some targetColor) None ]
    batchAdd s pileCmds
    runFor s 1.0

    // 4 boxes forming a cross shape
    let crossIds = [ for _ in 0..3 -> nextId "box" ]
    batchAdd s [
        makeBoxCmd crossIds.[0] (0.0, 6.0, 0.0) (0.6, 0.1, 0.1) 2.0 |> withColorAndMaterial (Some accentPurple) None   // horizontal bar
        makeBoxCmd crossIds.[1] (0.0, 6.0, 0.0) (0.1, 0.6, 0.1) 2.0 |> withColorAndMaterial (Some accentPurple) None   // vertical bar
        makeBoxCmd crossIds.[2] (0.0, 6.0, 0.0) (0.1, 0.1, 0.6) 2.0 |> withColorAndMaterial (Some accentPurple) None   // depth bar
        makeBoxCmd crossIds.[3] (0.0, 6.5, 0.0) (0.2, 0.2, 0.2) 3.0 |> withColorAndMaterial (Some accentPurple) None ] // center mass

    // Weld all bars to the horizontal bar so the cross moves as one rigid body
    batchAdd s [
        for i in 1..3 do
            makeWeldCmd (nextId "constraint") crossIds.[0] crossIds.[i] ]

    printfn "  Welded cross dropping onto pile..."
    runFor s 3.0

    setNarration s "Close-up — cross tumbles as one rigid body"
    smoothCamera s (2.0, 2.0, 4.0) (0.0, 1.0, 0.0) 1.5
    sleep 1700
    printfn "  Cross should tumble as one rigid body"
    runFor s 2.0
    clearNarration s
    printfn "  Constraints demo complete — 4 types: ball-socket, distance-limit, hinge, weld"

runStandalone "Constraints" run
