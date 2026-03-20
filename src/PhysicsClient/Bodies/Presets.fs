module PhysicsClient.Presets

open PhysicsClient.Session
open PhysicsClient.SimulationCommands

let marble (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 0.005
    addSphere session pos 0.01 m id

let bowlingBall (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 6.35
    addSphere session pos 0.11 m id

let beachBall (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 0.1
    addSphere session pos 0.2 m id

let crate (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 20.0
    addBox session pos (0.5, 0.5, 0.5) m id

let brick (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 3.0
    addBox session pos (0.2, 0.1, 0.05) m id

let boulder (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 200.0
    addSphere session pos 0.5 m id

let die (session: Session) (position: (float * float * float) option) (mass: float option) (id: string option) : Result<string, string> =
    let pos = position |> Option.defaultValue (0.0, 0.0, 0.0)
    let m = mass |> Option.defaultValue 0.03
    addBox session pos (0.05, 0.05, 0.05) m id
