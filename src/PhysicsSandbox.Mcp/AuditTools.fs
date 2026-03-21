/// <summary>MCP tool class for retrieving the recent command audit log showing all commands routed through the physics server.</summary>
module PhysicsSandbox.Mcp.AuditTools

open System.ComponentModel
open System.Text
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection
open PhysicsSandbox.Shared.Contracts

/// <summary>MCP server tool type for accessing the command audit trail.</summary>
[<McpServerToolType>]
type AuditTools() =

    /// <summary>Returns the most recent command events from the audit stream, formatted with human-readable descriptions of each simulation or view command.</summary>
    [<McpServerTool>]
    [<Description("Get the recent command log showing all commands sent through the server")>]
    static member get_command_log(connection: GrpcConnection, [<Description("Maximum number of entries to return (default 20)")>] count: int) =
        let maxCount = if count <= 0 then 20 else count
        let log = connection.CommandLog
        let entries = log |> List.rev |> List.truncate maxCount

        if entries.IsEmpty then
            "No commands recorded yet."
        else
            let sb = StringBuilder()
            sb.AppendLine($"Command Log (most recent {entries.Length} entries):") |> ignore
            sb.AppendLine("---") |> ignore
            for evt in entries do
                let desc =
                    if evt.SimulationCommand <> null then
                        let cmd = evt.SimulationCommand
                        if cmd.AddBody <> null then $"AddBody: id={cmd.AddBody.Id} mass={cmd.AddBody.Mass}"
                        elif cmd.ApplyForce <> null then $"ApplyForce: body={cmd.ApplyForce.BodyId}"
                        elif cmd.ApplyImpulse <> null then $"ApplyImpulse: body={cmd.ApplyImpulse.BodyId}"
                        elif cmd.ApplyTorque <> null then $"ApplyTorque: body={cmd.ApplyTorque.BodyId}"
                        elif cmd.SetGravity <> null then $"SetGravity: ({cmd.SetGravity.Gravity.X}, {cmd.SetGravity.Gravity.Y}, {cmd.SetGravity.Gravity.Z})"
                        elif cmd.Step <> null then "StepSimulation"
                        elif cmd.PlayPause <> null then $"PlayPause: running={cmd.PlayPause.Running}"
                        elif cmd.RemoveBody <> null then $"RemoveBody: body={cmd.RemoveBody.BodyId}"
                        elif cmd.ClearForces <> null then $"ClearForces: body={cmd.ClearForces.BodyId}"
                        else "Unknown simulation command"
                    elif evt.ViewCommand <> null then
                        let vcmd = evt.ViewCommand
                        if vcmd.SetCamera <> null then "SetCamera"
                        elif vcmd.SetZoom <> null then $"SetZoom: level={vcmd.SetZoom.Level}"
                        elif vcmd.ToggleWireframe <> null then $"ToggleWireframe: enabled={vcmd.ToggleWireframe.Enabled}"
                        else "Unknown view command"
                    else
                        "Empty event"
                sb.AppendLine($"  {desc}") |> ignore
            sb.ToString()
