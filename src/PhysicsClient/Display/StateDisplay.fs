module PhysicsClient.StateDisplay

open System
open Spectre.Console
open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Session

let internal formatVec3 (v: Vec3) =
    if isNull (box v) then "(0.00, 0.00, 0.00)"
    else $"({v.X:F2}, {v.Y:F2}, {v.Z:F2})"

let internal velocityMagnitude (v: Vec3) =
    if isNull (box v) then 0.0
    else sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z)

let internal shapeDescription (body: Body) =
    if isNull body.Shape then "Unknown"
    else
        match body.Shape.ShapeCase with
        | Shape.ShapeOneofCase.Sphere -> $"Sphere(r={body.Shape.Sphere.Radius:F2})"
        | Shape.ShapeOneofCase.Box ->
            let h = body.Shape.Box.HalfExtents
            $"Box({h.X:F2}\u00d7{h.Y:F2}\u00d7{h.Z:F2})"
        | Shape.ShapeOneofCase.Plane -> "Plane"
        | _ -> "Unknown"

let private stalenessInfo (session: Session) =
    let age = DateTime.UtcNow - lastStateUpdate session
    if age.TotalSeconds > 5.0 then
        AnsiConsole.MarkupLine($"[dim]Last updated: {age.TotalSeconds:F0}s ago[/]")

let listBodies (session: Session) =
    match latestState session with
    | None ->
        AnsiConsole.MarkupLine("[yellow]No simulation state available[/]")
    | Some state when state.Bodies.Count = 0 ->
        AnsiConsole.MarkupLine("[yellow]No bodies in simulation[/]")
        stalenessInfo session
    | Some state ->
        let table = Table()
        table.AddColumn("ID") |> ignore
        table.AddColumn("Shape") |> ignore
        table.AddColumn("Position") |> ignore
        table.AddColumn("Velocity") |> ignore
        table.AddColumn("Mass") |> ignore
        for body in state.Bodies do
            table.AddRow(
                body.Id,
                shapeDescription body,
                formatVec3 body.Position,
                formatVec3 body.Velocity,
                $"{body.Mass:F2}"
            ) |> ignore
        AnsiConsole.Write(table)
        stalenessInfo session

let inspect (session: Session) (bodyId: string) =
    match latestState session with
    | None -> AnsiConsole.MarkupLine("[yellow]No simulation state available[/]")
    | Some state ->
        let body = state.Bodies |> Seq.tryFind (fun b -> b.Id = bodyId)
        match body with
        | None -> AnsiConsole.MarkupLine($"[red]Body '{bodyId}' not found[/]")
        | Some b ->
            let orientStr =
                if isNull (box b.Orientation) then "(0.00, 0.00, 0.00, 0.00)"
                else $"({b.Orientation.X:F2}, {b.Orientation.Y:F2}, {b.Orientation.Z:F2}, {b.Orientation.W:F2})"
            let panel = Panel(
                $"Shape: {shapeDescription b}\n" +
                $"Position: {formatVec3 b.Position}\n" +
                $"Velocity: {formatVec3 b.Velocity}\n" +
                $"Angular Vel: {formatVec3 b.AngularVelocity}\n" +
                $"Orientation: {orientStr}\n" +
                $"Mass: {b.Mass:F2}")
            panel.Header <- PanelHeader($"[bold]{b.Id}[/]")
            AnsiConsole.Write(panel)

let status (session: Session) =
    match latestState session with
    | None -> AnsiConsole.MarkupLine("[yellow]No simulation state available[/]")
    | Some state ->
        let runLabel = if state.Running then "[green]RUNNING[/]" else "[yellow]PAUSED[/]"
        let panel = Panel(
            $"Time: {state.Time:F2}s\n" +
            $"Status: {runLabel}\n" +
            $"Bodies: {state.Bodies.Count}")
        panel.Header <- PanelHeader("[bold]Simulation Status[/]")
        AnsiConsole.Write(panel)
        stalenessInfo session

let snapshot (session: Session) =
    latestState session
