/// <summary>Predefined body presets that create common real-world objects with realistic dimensions and masses.</summary>
module PhysicsClient.Presets

open PhysicsClient.Session
open PhysicsClient.SimulationCommands

/// <summary>Creates a small glass marble (radius 0.01, default mass 0.005 kg).</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">Spawn position as (x, y, z); defaults to origin.</param>
/// <param name="mass">Override the default mass.</param>
/// <param name="id">Override the auto-generated body ID.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let marble (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 0.005
    addSphere session pos 0.01 m id None None None None None

/// <summary>Creates a bowling ball (radius 0.11, default mass 6.35 kg).</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">Spawn position as (x, y, z); defaults to origin.</param>
/// <param name="mass">Override the default mass.</param>
/// <param name="id">Override the auto-generated body ID.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let bowlingBall (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 6.35
    addSphere session pos 0.11 m id None None None None None

/// <summary>Creates a light beach ball (radius 0.2, default mass 0.1 kg).</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">Spawn position as (x, y, z); defaults to origin.</param>
/// <param name="mass">Override the default mass.</param>
/// <param name="id">Override the auto-generated body ID.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let beachBall (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 0.1
    addSphere session pos 0.2 m id None None None None None

/// <summary>Creates a wooden crate (half-extents 0.5x0.5x0.5, default mass 20 kg).</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">Spawn position as (x, y, z); defaults to origin.</param>
/// <param name="mass">Override the default mass.</param>
/// <param name="id">Override the auto-generated body ID.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let crate (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 20.0
    addBox session pos (0.5, 0.5, 0.5) m id None None None None None

/// <summary>Creates a standard brick (half-extents 0.2x0.1x0.05, default mass 3 kg).</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">Spawn position as (x, y, z); defaults to origin.</param>
/// <param name="mass">Override the default mass.</param>
/// <param name="id">Override the auto-generated body ID.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let brick (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 3.0
    addBox session pos (0.2, 0.1, 0.05) m id None None None None None

/// <summary>Creates a heavy boulder (radius 0.5, default mass 200 kg).</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">Spawn position as (x, y, z); defaults to origin.</param>
/// <param name="mass">Override the default mass.</param>
/// <param name="id">Override the auto-generated body ID.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let boulder (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 200.0
    addSphere session pos 0.5 m id None None None None None

/// <summary>Creates a small die cube (half-extents 0.05x0.05x0.05, default mass 0.03 kg).</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">Spawn position as (x, y, z); defaults to origin.</param>
/// <param name="mass">Override the default mass.</param>
/// <param name="id">Override the auto-generated body ID.</param>
/// <returns>Ok with the assigned body ID, or Error with a failure message.</returns>
let die (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 0.03
    addBox session pos (0.05, 0.05, 0.05) m id None None None None None
