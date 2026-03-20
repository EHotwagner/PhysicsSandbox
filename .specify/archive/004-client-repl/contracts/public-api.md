# Public API Contract: PhysicsClient Library

**Feature**: 004-client-repl | **Date**: 2026-03-20

This defines the public API surface of the PhysicsClient library as exposed through `.fsi` signature files. These signatures serve as the contract between the library and its users (FSI sessions, scripts, tests).

## Module: PhysicsClient.Session

```fsharp
module PhysicsClient.Session

open System.Threading

/// Opaque session handle for all library operations.
type Session

/// Connect to the physics server. Returns a session or error.
val connect : serverAddress: string -> Result<Session, string>

/// Disconnect and clean up resources.
val disconnect : session: Session -> unit

/// Reconnect a disconnected session to the same server.
val reconnect : session: Session -> Result<Session, string>

/// Check if the session is currently connected.
val isConnected : session: Session -> bool
```

## Module: PhysicsClient.SimulationCommands

```fsharp
module PhysicsClient.SimulationCommands

open PhysicsClient.Session

/// Add a sphere body. Returns body ID or error.
val addSphere : session: Session -> position: (float * float * float) -> radius: float -> mass: float -> ?id: string -> Result<string, string>

/// Add a box body. Returns body ID or error.
val addBox : session: Session -> position: (float * float * float) -> halfExtents: (float * float * float) -> mass: float -> ?id: string -> Result<string, string>

/// Add a static ground plane.
val addPlane : session: Session -> ?normal: (float * float * float) -> ?id: string -> Result<string, string>

/// Remove a body by ID.
val removeBody : session: Session -> bodyId: string -> Result<unit, string>

/// Remove all bodies created by this session.
val clearAll : session: Session -> Result<int, string>

/// Apply a persistent force to a body.
val applyForce : session: Session -> bodyId: string -> force: (float * float * float) -> Result<unit, string>

/// Apply an instantaneous impulse to a body.
val applyImpulse : session: Session -> bodyId: string -> impulse: (float * float * float) -> Result<unit, string>

/// Apply a torque to a body.
val applyTorque : session: Session -> bodyId: string -> torque: (float * float * float) -> Result<unit, string>

/// Clear all forces on a body.
val clearForces : session: Session -> bodyId: string -> Result<unit, string>

/// Set global gravity vector.
val setGravity : session: Session -> gravity: (float * float * float) -> Result<unit, string>

/// Start simulation playback.
val play : session: Session -> Result<unit, string>

/// Pause simulation playback.
val pause : session: Session -> Result<unit, string>

/// Advance simulation by one step.
val step : session: Session -> Result<unit, string>
```

## Module: PhysicsClient.ViewCommands

```fsharp
module PhysicsClient.ViewCommands

open PhysicsClient.Session

/// Set camera position and look-at target.
val setCamera : session: Session -> position: (float * float * float) -> target: (float * float * float) -> Result<unit, string>

/// Set zoom level.
val setZoom : session: Session -> level: float -> Result<unit, string>

/// Toggle wireframe rendering.
val wireframe : session: Session -> enabled: bool -> Result<unit, string>
```

## Module: PhysicsClient.Presets

```fsharp
module PhysicsClient.Presets

open PhysicsClient.Session

/// Add a marble (small sphere, 0.005 kg).
val marble : session: Session -> ?position: (float * float * float) -> ?mass: float -> Result<string, string>

/// Add a bowling ball (sphere, 6.35 kg).
val bowlingBall : session: Session -> ?position: (float * float * float) -> ?mass: float -> Result<string, string>

/// Add a beach ball (light sphere, 0.1 kg).
val beachBall : session: Session -> ?position: (float * float * float) -> ?mass: float -> Result<string, string>

/// Add a crate (box, 20 kg).
val crate : session: Session -> ?position: (float * float * float) -> ?mass: float -> Result<string, string>

/// Add a brick (small box, 3 kg).
val brick : session: Session -> ?position: (float * float * float) -> ?mass: float -> Result<string, string>

/// Add a boulder (large sphere, 200 kg).
val boulder : session: Session -> ?position: (float * float * float) -> ?mass: float -> Result<string, string>

/// Add a die (tiny box, 0.03 kg).
val die : session: Session -> ?position: (float * float * float) -> ?mass: float -> Result<string, string>
```

## Module: PhysicsClient.Generators

```fsharp
module PhysicsClient.Generators

open PhysicsClient.Session

/// Generate N random spheres with varied size, mass, and position.
val randomSpheres : session: Session -> count: int -> ?seed: int -> Result<string list, string>

/// Generate N random boxes with varied dimensions, mass, and position.
val randomBoxes : session: Session -> count: int -> ?seed: int -> Result<string list, string>

/// Generate N random mixed bodies.
val randomBodies : session: Session -> count: int -> ?seed: int -> Result<string list, string>

/// Create a vertical stack of N boxes.
val stack : session: Session -> count: int -> ?position: (float * float * float) -> Result<string list, string>

/// Create a row of N spheres along the X axis.
val row : session: Session -> count: int -> ?position: (float * float * float) -> Result<string list, string>

/// Create an N×M grid of boxes on the ground plane.
val grid : session: Session -> rows: int -> cols: int -> ?position: (float * float * float) -> Result<string list, string>

/// Create a pyramid of N layers of boxes.
val pyramid : session: Session -> layers: int -> ?position: (float * float * float) -> Result<string list, string>
```

## Module: PhysicsClient.Steering

```fsharp
module PhysicsClient.Steering

open PhysicsClient.Session

type Direction = Up | Down | North | South | East | West

/// Push a body in a named direction with given magnitude.
val push : session: Session -> bodyId: string -> direction: Direction -> magnitude: float -> Result<unit, string>

/// Push a body using a raw vector.
val pushVec : session: Session -> bodyId: string -> vector: (float * float * float) -> Result<unit, string>

/// Launch a body toward a target position with given speed.
val launch : session: Session -> bodyId: string -> target: (float * float * float) -> speed: float -> Result<unit, string>

/// Apply spin (torque) around a named axis.
val spin : session: Session -> bodyId: string -> axis: Direction -> magnitude: float -> Result<unit, string>

/// Stop a body (clear forces + counter-impulse to zero velocity).
val stop : session: Session -> bodyId: string -> Result<unit, string>
```

## Module: PhysicsClient.StateDisplay

```fsharp
module PhysicsClient.StateDisplay

open PhysicsClient.Session

/// Print a formatted table of all bodies.
val listBodies : session: Session -> unit

/// Print detailed info for a single body.
val inspect : session: Session -> bodyId: string -> unit

/// Print simulation status (time, running/paused, body count).
val status : session: Session -> unit

/// Get the raw latest state snapshot.
val snapshot : session: Session -> PhysicsSandbox.Shared.Contracts.SimulationState option
```

## Module: PhysicsClient.LiveWatch

```fsharp
module PhysicsClient.LiveWatch

open PhysicsClient.Session

/// Start live-watch mode. Prints state updates until cancelled (Ctrl+C returns to REPL).
/// Filter options: body IDs, shape type, minimum velocity threshold.
val watch : session: Session -> ?bodyIds: string list -> ?shapeFilter: string -> ?minVelocity: float -> unit
```
