module PhysicsClient.Generators

open System
open PhysicsClient.Session
open PhysicsClient.SimulationCommands

let private nextDouble (rng: Random) (min: float) (max: float) =
    min + rng.NextDouble() * (max - min)

let randomSpheres (session: Session) (count: int) (seed: int option) : Result<string list, string> =
    if count <= 0 then
        Error "count must be greater than 0"
    else
        let rng = seed |> Option.map (fun s -> Random(s)) |> Option.defaultWith (fun () -> Random())
        let mutable ids = []
        let mutable lastError = None
        for _ in 1..count do
            if lastError.IsNone then
                let x = nextDouble rng -5.0 5.0
                let y = nextDouble rng 1.0 10.0
                let z = nextDouble rng -5.0 5.0
                let radius = nextDouble rng 0.05 0.5
                let mass = nextDouble rng 0.1 50.0
                match addSphere session (x, y, z) radius mass None with
                | Ok id -> ids <- id :: ids
                | Error e -> lastError <- Some e
        match lastError with
        | Some e -> Error e
        | None -> Ok (List.rev ids)

let randomBoxes (session: Session) (count: int) (seed: int option) : Result<string list, string> =
    if count <= 0 then
        Error "count must be greater than 0"
    else
        let rng = seed |> Option.map (fun s -> Random(s)) |> Option.defaultWith (fun () -> Random())
        let mutable ids = []
        let mutable lastError = None
        for _ in 1..count do
            if lastError.IsNone then
                let x = nextDouble rng -5.0 5.0
                let y = nextDouble rng 1.0 10.0
                let z = nextDouble rng -5.0 5.0
                let hx = nextDouble rng 0.05 0.5
                let hy = nextDouble rng 0.05 0.5
                let hz = nextDouble rng 0.05 0.5
                let mass = nextDouble rng 0.1 50.0
                match addBox session (x, y, z) (hx, hy, hz) mass None with
                | Ok id -> ids <- id :: ids
                | Error e -> lastError <- Some e
        match lastError with
        | Some e -> Error e
        | None -> Ok (List.rev ids)

let randomBodies (session: Session) (count: int) (seed: int option) : Result<string list, string> =
    if count <= 0 then
        Error "count must be greater than 0"
    else
        let rng = seed |> Option.map (fun s -> Random(s)) |> Option.defaultWith (fun () -> Random())
        let mutable ids = []
        let mutable lastError = None
        for _ in 1..count do
            if lastError.IsNone then
                let x = nextDouble rng -5.0 5.0
                let y = nextDouble rng 1.0 10.0
                let z = nextDouble rng -5.0 5.0
                let mass = nextDouble rng 0.1 50.0
                let isSphere = rng.Next(2) = 0
                let result =
                    if isSphere then
                        let radius = nextDouble rng 0.05 0.5
                        addSphere session (x, y, z) radius mass None
                    else
                        let hx = nextDouble rng 0.05 0.5
                        let hy = nextDouble rng 0.05 0.5
                        let hz = nextDouble rng 0.05 0.5
                        addBox session (x, y, z) (hx, hy, hz) mass None
                match result with
                | Ok id -> ids <- id :: ids
                | Error e -> lastError <- Some e
        match lastError with
        | Some e -> Error e
        | None -> Ok (List.rev ids)

let stack (session: Session) (count: int) (position: (float * float * float) option) : Result<string list, string> =
    if count <= 0 then
        Error "count must be greater than 0"
    else
        let (bx, by, bz) = position |> Option.defaultValue (0.0, 0.0, 0.0)
        let mutable ids = []
        let mutable lastError = None
        for i in 0..(count - 1) do
            if lastError.IsNone then
                let y = by + (float i) * 1.0 + 0.5
                match addBox session (bx, y, bz) (0.5, 0.5, 0.5) 20.0 None with
                | Ok id -> ids <- id :: ids
                | Error e -> lastError <- Some e
        match lastError with
        | Some e -> Error e
        | None -> Ok (List.rev ids)

let row (session: Session) (count: int) (position: (float * float * float) option) : Result<string list, string> =
    if count <= 0 then
        Error "count must be greater than 0"
    else
        let (bx, by, bz) = position |> Option.defaultValue (0.0, 0.0, 0.0)
        let mutable ids = []
        let mutable lastError = None
        for i in 0..(count - 1) do
            if lastError.IsNone then
                let x = bx + (float i) * 0.5
                match addSphere session (x, by, bz) 0.2 1.0 None with
                | Ok id -> ids <- id :: ids
                | Error e -> lastError <- Some e
        match lastError with
        | Some e -> Error e
        | None -> Ok (List.rev ids)

let grid (session: Session) (rows: int) (cols: int) (position: (float * float * float) option) : Result<string list, string> =
    if rows <= 0 || cols <= 0 then
        Error "rows and cols must be greater than 0"
    else
        let (bx, by, bz) = position |> Option.defaultValue (0.0, 0.0, 0.0)
        let mutable ids = []
        let mutable lastError = None
        for r in 0..(rows - 1) do
            for c in 0..(cols - 1) do
                if lastError.IsNone then
                    let x = bx + (float c) * 1.0
                    let z = bz + (float r) * 1.0
                    match addBox session (x, by + 0.5, z) (0.5, 0.5, 0.5) 20.0 None with
                    | Ok id -> ids <- id :: ids
                    | Error e -> lastError <- Some e
        match lastError with
        | Some e -> Error e
        | None -> Ok (List.rev ids)

let pyramid (session: Session) (layers: int) (position: (float * float * float) option) : Result<string list, string> =
    if layers <= 0 then
        Error "layers must be greater than 0"
    else
        let (bx, by, bz) = position |> Option.defaultValue (0.0, 0.0, 0.0)
        let mutable ids = []
        let mutable lastError = None
        for i in 0..(layers - 1) do
            let width = layers - i
            let offsetX = (float i) * 0.5
            let y = by + (float i) * 1.0 + 0.5
            for j in 0..(width - 1) do
                if lastError.IsNone then
                    let x = bx + offsetX + (float j) * 1.0
                    match addBox session (x, y, bz) (0.5, 0.5, 0.5) 20.0 None with
                    | Ok id -> ids <- id :: ids
                    | Error e -> lastError <- Some e
        match lastError with
        | Some e -> Error e
        | None -> Ok (List.rev ids)
