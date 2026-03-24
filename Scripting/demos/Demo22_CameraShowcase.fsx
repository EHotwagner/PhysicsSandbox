// Demo 22: Camera Showcase — ~40 seconds showcasing all smooth camera modes and narration.
// Usage: dotnet fsi Scripting/demos/Demo22_CameraShowcase.fsx [server-address]

#load "Prelude.fsx"
open Prelude
open PhysicsClient.Session
open PhysicsClient.SimulationCommands
open PhysicsClient.ViewCommands
open PhysicsClient.Presets

let name = "Camera Showcase"

let run s =
    resetSimulation s
    setDemoInfo s "Demo 22: Camera Showcase" "Smooth camera transitions, body tracking, orbit, chase, framing, and shake."

    // Build scene: several shapes spread out
    let ballId = nextId "sphere"
    batchAdd s [ makeSphereCmd ballId (0.0, 8.0, 0.0) 0.5 5.0
                 |> withColorAndMaterial (Some projectileColor) None ]
    let crateId = nextId "box"
    batchAdd s [ makeBoxCmd crateId (4.0, 10.0, 0.0) (0.4, 0.4, 0.4) 10.0
                 |> withColorAndMaterial (Some targetColor) None ]
    let capsuleId = nextId "capsule"
    batchAdd s [ makeCapsuleCmd capsuleId (-3.0, 12.0, 2.0) 0.3 0.8 4.0
                 |> withColorAndMaterial (Some accentGreen) None ]

    // Start simulation so bodies are moving from the start
    play s |> ignore
    sleep 500

    // 1. Smooth camera: wide establishing shot
    setNarration s "Smooth Camera — gliding to an establishing shot"
    sleep 100
    smoothCamera s (15.0, 10.0, 15.0) (0.0, 3.0, 0.0) 2.0
    sleep 2500

    // 2. Smooth move to close-up while things fall
    setNarration s "Smooth Camera — zooming in to the action"
    sleep 100
    smoothCamera s (3.0, 3.0, 6.0) (0.0, 1.0, 0.0) 1.5
    sleep 2000

    // 3. LookAt: orient toward the ball
    setNarration s "LookAt — smoothly orienting toward the red ball"
    sleep 100
    lookAtBody s ballId 1.5
    sleep 2000

    // 4. Follow: launch the ball hard and track it
    setNarration s "Follow — tracking the red ball as it flies!"
    sleep 100
    batchAdd s [ makeImpulseCmd ballId (8.0, 40.0, 5.0) ]
    followBody s ballId
    sleep 3500

    // 5. Chase: launch the crate sideways and chase it
    stopCamera s
    sleep 100
    setNarration s "Chase — chasing the blue crate with offset"
    sleep 100
    batchAdd s [ makeImpulseCmd crateId (-20.0, 15.0, 8.0) ]
    chaseBody s crateId (5.0, 4.0, 7.0)
    sleep 3500

    // 6. Pull back for orbit
    stopCamera s
    sleep 100
    setNarration s "Smooth Camera — pulling back for orbit view"
    sleep 100
    smoothCamera s (10.0, 6.0, 10.0) (0.0, 1.0, 0.0) 1.5
    sleep 2000

    // 7. Orbit: revolve around the ball
    setNarration s "Orbit — revolving 360 degrees around the red ball"
    sleep 100
    orbitBody s ballId 5.0 360.0
    sleep 5500

    // 8. Frame all bodies
    stopCamera s
    sleep 100
    setNarration s "Frame Bodies — auto-positioning to show all objects"
    sleep 100
    frameBodies s [ballId; crateId; capsuleId]
    sleep 3000

    // 9. Shake on impact
    setNarration s "Camera Shake — impact effect!"
    sleep 100
    batchAdd s [ makeImpulseCmd capsuleId (0.0, 30.0, 0.0) ]
    shakeCamera s 0.3 1.0
    sleep 1500

    // 10. Final establishing shot
    stopCamera s
    sleep 100
    setNarration s "Final shot — smooth glide to overview"
    sleep 100
    smoothCamera s (12.0, 8.0, 12.0) (0.0, 1.0, 0.0) 2.0
    sleep 2500

    pause s |> ignore
    clearNarration s
    setNarration s "Camera Showcase complete!"
    sleep 2000
    clearNarration s
    printfn "  Camera showcase complete — ~40 seconds of cinematic camera work"

runStandalone name run
