/// <summary>Displays simulation state in the terminal using Spectre.Console tables and panels.</summary>
module PhysicsClient.StateDisplay

open System
open Spectre.Console
open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Session

/// <summary>Formats a Vec3 as a human-readable string with two decimal places, e.g. "(1.23, 4.56, 7.89)".</summary>
let internal formatVec3 (v: Vec3) =
    if isNull (box v) then "(0.00, 0.00, 0.00)"
    else $"({v.X:F2}, {v.Y:F2}, {v.Z:F2})"

/// <summary>Computes the scalar speed (magnitude) of a velocity vector.</summary>
let internal velocityMagnitude (v: Vec3) =
    if isNull (box v) then 0.0
    else sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z)

/// <summary>Describes a shape proto, resolving cached refs if a resolver is provided.</summary>
let rec private describeShape (resolver: MeshResolver.MeshResolverState option) (shape: Shape) =
    if isNull shape then "Unknown"
    else
        match shape.ShapeCase with
        | Shape.ShapeOneofCase.Sphere -> $"Sphere(r={shape.Sphere.Radius:F2})"
        | Shape.ShapeOneofCase.Box ->
            let h = shape.Box.HalfExtents
            $"Box({h.X:F2}\u00d7{h.Y:F2}\u00d7{h.Z:F2})"
        | Shape.ShapeOneofCase.Plane -> "Plane"
        | Shape.ShapeOneofCase.CachedRef ->
            let meshId = shape.CachedRef.MeshId
            match resolver with
            | Some r ->
                match MeshResolver.resolve meshId r with
                | Some resolved -> describeShape None resolved
                | None -> $"Cached({meshId.[..7]})"
            | None -> $"Cached({meshId.[..7]})"
        | Shape.ShapeOneofCase.ConvexHull -> $"ConvexHull({shape.ConvexHull.Points.Count}pts)"
        | Shape.ShapeOneofCase.Mesh -> $"Mesh({shape.Mesh.Triangles.Count}tri)"
        | Shape.ShapeOneofCase.Compound -> $"Compound({shape.Compound.Children.Count})"
        | Shape.ShapeOneofCase.Capsule -> $"Capsule(r={shape.Capsule.Radius:F2},l={shape.Capsule.Length:F2})"
        | Shape.ShapeOneofCase.Cylinder -> $"Cylinder(r={shape.Cylinder.Radius:F2},l={shape.Cylinder.Length:F2})"
        | Shape.ShapeOneofCase.Triangle -> "Triangle"
        | _ -> "Unknown"

/// <summary>Returns a short description of a body's shape, e.g. "Sphere(r=0.50)" or "Box(1.00x1.00x1.00)".</summary>
let internal shapeDescription (body: Body) =
    describeShape None body.Shape

/// <summary>Returns a short description of a body's shape, resolving CachedShapeRef from the session's mesh resolver.</summary>
let internal shapeDescriptionResolved (session: Session) (body: Body) =
    describeShape (Some (meshResolver session)) body.Shape

let private stalenessInfo (session: Session) =
    let age = DateTime.UtcNow - lastStateUpdate session
    if age.TotalSeconds > 5.0 then
        AnsiConsole.MarkupLine($"[dim]Last updated: {age.TotalSeconds:F0}s ago[/]")

/// <summary>Prints a table of all bodies in the simulation with their shape, position, velocity, and mass.</summary>
/// <param name="session">The active server session.</param>
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
                shapeDescriptionResolved session body,
                formatVec3 body.Position,
                formatVec3 body.Velocity,
                $"{body.Mass:F2}"
            ) |> ignore
        AnsiConsole.Write(table)
        stalenessInfo session

/// <summary>Prints a detailed panel for a single body showing all its properties including orientation.</summary>
/// <param name="session">The active server session.</param>
/// <param name="bodyId">The ID of the body to inspect.</param>
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
                $"Shape: {shapeDescriptionResolved session b}\n" +
                $"Position: {formatVec3 b.Position}\n" +
                $"Velocity: {formatVec3 b.Velocity}\n" +
                $"Angular Vel: {formatVec3 b.AngularVelocity}\n" +
                $"Orientation: {orientStr}\n" +
                $"Mass: {b.Mass:F2}")
            panel.Header <- PanelHeader($"[bold]{b.Id}[/]")
            AnsiConsole.Write(panel)

/// <summary>Prints a summary panel showing simulation time, running/paused status, and total body count.</summary>
/// <param name="session">The active server session.</param>
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

/// <summary>Returns the latest cached simulation state without any console output.</summary>
/// <param name="session">The active server session.</param>
/// <returns>The most recent SimulationState if available, or None if no state has been received yet.</returns>
let snapshot (session: Session) =
    latestState session
