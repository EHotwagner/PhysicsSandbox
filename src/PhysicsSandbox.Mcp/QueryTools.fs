/// <summary>MCP tool class for querying simulation state and server connection health.</summary>
module PhysicsSandbox.Mcp.QueryTools

open System
open System.ComponentModel
open System.Text
open ModelContextProtocol.Server
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Mcp.GrpcConnection

let private formatShape (body: Body) =
    match body.Shape.ShapeCase with
    | Shape.ShapeOneofCase.Sphere -> $"Sphere(r={body.Shape.Sphere.Radius:F1})"
    | Shape.ShapeOneofCase.Box ->
        let h = body.Shape.Box.HalfExtents
        $"Box({h.X * 2.0:F0}x{h.Y * 2.0:F0}x{h.Z * 2.0:F0})"
    | Shape.ShapeOneofCase.Plane -> "Plane"
    | _ -> "Unknown"

let private formatState (conn: GrpcConnection) =
    match conn.LatestState with
    | None -> "No simulation state available (stream not yet received data)"
    | Some state ->
        let staleness = DateTimeOffset.UtcNow - conn.LastUpdateTime
        let sb = StringBuilder()
        sb.AppendLine($"Simulation State (cached {staleness.TotalSeconds:F1}s ago)") |> ignore
        sb.AppendLine($"Time: {state.Time:F3}s | Running: {state.Running} | Bodies: {state.Bodies.Count}") |> ignore
        if state.Bodies.Count > 0 then
            sb.AppendLine() |> ignore
            sb.AppendLine("  ID              | Position            | Velocity            | Mass  | Shape") |> ignore
            for body in state.Bodies do
                let p = body.Position
                let v = body.Velocity
                let id = body.Id.PadRight(16)
                sb.AppendLine($"  {id}| ({p.X:F1}, {p.Y:F1}, {p.Z:F1})".PadRight(42) + $"| ({v.X:F1}, {v.Y:F1}, {v.Z:F1})".PadRight(22) + $"| {body.Mass:F1}".PadRight(8) + $"| {formatShape body}") |> ignore
        sb.ToString().TrimEnd()

/// <summary>MCP server tool type for querying simulation state snapshots and server connection status.</summary>
[<McpServerToolType>]
type QueryTools() =

    /// <summary>Returns the current simulation state including all body positions, velocities, masses, and shapes from the cached background stream data.</summary>
    [<McpServerTool; Description("Get the current simulation state (bodies, time, running status). Returns cached data from background stream.")>]
    static member get_state(conn: GrpcConnection) : string =
        formatState conn

    /// <summary>Returns the MCP server's connection health including state stream, view stream, and audit stream connectivity and data staleness.</summary>
    [<McpServerTool; Description("Get MCP server connection status and health.")>]
    static member get_status(conn: GrpcConnection) : string =
        let staleness = DateTimeOffset.UtcNow - conn.LastUpdateTime
        let streamStatus =
            if conn.StreamConnected then $"connected (last update {staleness.TotalSeconds:F1}s ago)"
            else $"disconnected (last update {staleness.TotalSeconds:F1}s ago)"
        let viewStreamStatus =
            if conn.ViewStreamConnected then "connected"
            else "disconnected"
        let auditStreamStatus =
            if conn.AuditStreamConnected then "connected"
            else "disconnected"
        let hasState = conn.LatestState.IsSome
        $"MCP Server Status\nServer: {conn.ServerAddress}\nState Stream: {streamStatus}\nView Command Stream: {viewStreamStatus}\nAudit Stream: {auditStreamStatus}\nHas State Data: {hasState}"
