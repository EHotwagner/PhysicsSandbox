/// <summary>Provides a live-updating terminal display of simulation body state, refreshing at ~10 Hz.</summary>
module PhysicsClient.LiveWatch

open System
open System.Threading
open Spectre.Console
open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Session
open PhysicsClient.StateDisplay

let private resolveShapeCase (resolver: MeshResolver.MeshResolverState) (body: Body) =
    if isNull body.Shape then Shape.ShapeOneofCase.None
    elif body.Shape.ShapeCase = Shape.ShapeOneofCase.CachedRef then
        match MeshResolver.resolve body.Shape.CachedRef.MeshId resolver with
        | Some resolved -> resolved.ShapeCase
        | None -> Shape.ShapeOneofCase.CachedRef
    else body.Shape.ShapeCase

let private matchesShape (resolver: MeshResolver.MeshResolverState) (filter: string) (body: Body) =
    if isNull body.Shape then false
    else
        let shapeCase = resolveShapeCase resolver body
        match filter.ToLowerInvariant() with
        | "sphere" -> shapeCase = Shape.ShapeOneofCase.Sphere
        | "box" -> shapeCase = Shape.ShapeOneofCase.Box
        | "plane" -> shapeCase = Shape.ShapeOneofCase.Plane
        | "convexhull" | "hull" -> shapeCase = Shape.ShapeOneofCase.ConvexHull
        | "mesh" -> shapeCase = Shape.ShapeOneofCase.Mesh
        | "compound" -> shapeCase = Shape.ShapeOneofCase.Compound
        | _ -> true

let private filterBodies
    (resolver: MeshResolver.MeshResolverState)
    (bodyIds: string list option)
    (shapeFilter: string option)
    (minVelocity: float option)
    (bodies: seq<Body>) =
    bodies
    |> Seq.filter (fun b ->
        let idMatch =
            match bodyIds with
            | None | Some [] -> true
            | Some ids -> ids |> List.exists (fun id -> id = b.Id)
        let shapeMatch =
            match shapeFilter with
            | None -> true
            | Some f -> matchesShape resolver f b
        let velMatch =
            match minVelocity with
            | None -> true
            | Some mv -> velocityMagnitude b.Velocity >= mv
        idMatch && shapeMatch && velMatch)
    |> Seq.toList

let private renderTable (resolver: MeshResolver.MeshResolverState) (state: SimulationState option) bodyIds shapeFilter minVelocity =
    let table = Table()
    table.Title <- TableTitle("[bold]Live Watch[/] [dim](Ctrl+C to stop)[/]")
    table.AddColumn("ID") |> ignore
    table.AddColumn("Shape") |> ignore
    table.AddColumn("Position") |> ignore
    table.AddColumn("Velocity") |> ignore
    table.AddColumn("Speed") |> ignore
    table.AddColumn("Mass") |> ignore
    match state with
    | None ->
        table.AddRow("[dim]Waiting for state...[/]", "", "", "", "", "") |> ignore
    | Some s ->
        let filtered = filterBodies resolver bodyIds shapeFilter minVelocity s.Bodies
        if filtered.IsEmpty then
            table.AddRow("[dim]No matching bodies[/]", "", "", "", "", "") |> ignore
        else
            for body in filtered do
                let speed = velocityMagnitude body.Velocity
                table.AddRow(
                    body.Id,
                    shapeDescription body,
                    formatVec3 body.Position,
                    formatVec3 body.Velocity,
                    $"{speed:F2}",
                    $"{body.Mass:F2}"
                ) |> ignore
        let runLabel = if s.Running then "[green]RUNNING[/]" else "[yellow]PAUSED[/]"
        table.Caption <- TableTitle($"Time: {s.Time:F2}s | {runLabel} | Bodies: {s.Bodies.Count}")
    table

/// <summary>Starts a live-updating table in the terminal that continuously displays body state. Press Ctrl+C to stop.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyIds">Optional list of body IDs to watch; None shows all bodies.</param>
/// <param name="shapeFilter">Optional shape type filter (e.g., "sphere", "box", "plane").</param>
/// <param name="minVelocity">Optional minimum velocity threshold; only bodies moving at least this fast are shown.</param>
let watch (session: Session) (bodyIds: string list option) (shapeFilter: string option) (minVelocity: float option) =
    use cts = new CancellationTokenSource()
    let originalHandler = Console.CancelKeyPress
    Console.CancelKeyPress.Add(fun args ->
        args.Cancel <- true
        cts.Cancel())
    try
        AnsiConsole.Live(Table())
            .Start(fun ctx ->
                while not cts.Token.IsCancellationRequested do
                    let state = latestState session
                    let table = renderTable (meshResolver session) state bodyIds shapeFilter minVelocity
                    ctx.UpdateTarget(table)
                    try
                        Thread.Sleep(100)
                    with
                    | :? OperationCanceledException -> ())
    with
    | :? OperationCanceledException -> ()
    AnsiConsole.MarkupLine("[dim]Watch stopped.[/]")
