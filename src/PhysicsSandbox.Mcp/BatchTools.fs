namespace PhysicsSandbox.Mcp

open System.ComponentModel
open System.Text
open System.Text.Json
open System.Threading.Tasks
open ModelContextProtocol.Server
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Mcp.GrpcConnection

/// <summary>MCP server tool type for submitting multiple simulation or view commands in a single batch call, reducing round-trip overhead.</summary>
[<McpServerToolType>]
type BatchTools() =

    static let tryGetDouble (elem: JsonElement) (name: string) (defaultVal: float) =
        let mutable prop = JsonElement()
        if elem.TryGetProperty(name, &prop) && prop.ValueKind = JsonValueKind.Number then
            prop.GetDouble()
        else
            defaultVal

    static let tryGetString (elem: JsonElement) (name: string) (defaultVal: string) =
        let mutable prop = JsonElement()
        if elem.TryGetProperty(name, &prop) && prop.ValueKind = JsonValueKind.String then
            prop.GetString()
        else
            defaultVal

    static let tryGetBool (elem: JsonElement) (name: string) (defaultVal: bool) =
        let mutable prop = JsonElement()
        if elem.TryGetProperty(name, &prop) then
            match prop.ValueKind with
            | JsonValueKind.True -> true
            | JsonValueKind.False -> false
            | _ -> defaultVal
        else
            defaultVal

    static let parseSimCommand (elem: JsonElement) : SimulationCommand option =
        let cmd = SimulationCommand()
        match tryGetString elem "type" "" with
        | "add_body" ->
            let body = AddBody()
            let shape = tryGetString elem "shape" "sphere"
            let id = tryGetString elem "id" ""
            body.Id <- if id = "" then PhysicsClient.IdGenerator.nextId shape else id
            body.Position <- Vec3(X = tryGetDouble elem "x" 0.0, Y = tryGetDouble elem "y" 0.0, Z = tryGetDouble elem "z" 0.0)
            body.Mass <- tryGetDouble elem "mass" 1.0
            let shape = Shape()
            match tryGetString elem "shape" "sphere" with
            | "box" ->
                shape.Box <- Box(HalfExtents = Vec3(X = tryGetDouble elem "hx" 0.5, Y = tryGetDouble elem "hy" 0.5, Z = tryGetDouble elem "hz" 0.5))
            | "plane" ->
                shape.Plane <- Plane(Normal = Vec3(X = 0.0, Y = 1.0, Z = 0.0))
                body.Mass <- 0.0
            | _ ->
                shape.Sphere <- Sphere(Radius = tryGetDouble elem "radius" 0.5)
            body.Shape <- shape
            cmd.AddBody <- body
            Some cmd
        | "apply_force" ->
            cmd.ApplyForce <- ApplyForce(BodyId = tryGetString elem "body_id" "", Force = Vec3(X = tryGetDouble elem "fx" 0.0, Y = tryGetDouble elem "fy" 0.0, Z = tryGetDouble elem "fz" 0.0))
            Some cmd
        | "apply_impulse" ->
            cmd.ApplyImpulse <- ApplyImpulse(BodyId = tryGetString elem "body_id" "", Impulse = Vec3(X = tryGetDouble elem "fx" 0.0, Y = tryGetDouble elem "fy" 0.0, Z = tryGetDouble elem "fz" 0.0))
            Some cmd
        | "step" ->
            cmd.Step <- StepSimulation(DeltaTime = 0.0)
            Some cmd
        | "play" ->
            cmd.PlayPause <- PlayPause(Running = true)
            Some cmd
        | "pause" ->
            cmd.PlayPause <- PlayPause(Running = false)
            Some cmd
        | "set_gravity" ->
            cmd.SetGravity <- SetGravity(Gravity = Vec3(X = tryGetDouble elem "x" 0.0, Y = tryGetDouble elem "y" -9.81, Z = tryGetDouble elem "z" 0.0))
            Some cmd
        | "remove_body" ->
            cmd.RemoveBody <- RemoveBody(BodyId = tryGetString elem "body_id" "")
            Some cmd
        | "clear_forces" ->
            cmd.ClearForces <- ClearForces(BodyId = tryGetString elem "body_id" "")
            Some cmd
        | "reset" ->
            cmd.Reset <- ResetSimulation()
            Some cmd
        | _ -> None

    static let parseViewCommand (elem: JsonElement) : ViewCommand option =
        let cmd = ViewCommand()
        match tryGetString elem "type" "" with
        | "set_camera" ->
            let cam = SetCamera()
            cam.Position <- Vec3(X = tryGetDouble elem "px" 0.0, Y = tryGetDouble elem "py" 10.0, Z = tryGetDouble elem "pz" 20.0)
            cam.Target <- Vec3(X = tryGetDouble elem "tx" 0.0, Y = tryGetDouble elem "ty" 0.0, Z = tryGetDouble elem "tz" 0.0)
            cmd.SetCamera <- cam
            Some cmd
        | "set_zoom" ->
            cmd.SetZoom <- SetZoom(Level = tryGetDouble elem "level" 1.0)
            Some cmd
        | "toggle_wireframe" ->
            cmd.ToggleWireframe <- ToggleWireframe(Enabled = tryGetBool elem "enabled" true)
            Some cmd
        | _ -> None

    static let formatBatchResponse (response: BatchResponse) (parseErrors: int) (label: string) =
        let sb = StringBuilder()
        sb.AppendLine($"Batch: {response.Results.Count} {label} processed in {response.TotalTimeMs:F1}ms") |> ignore
        if parseErrors > 0 then
            sb.AppendLine($"  ({parseErrors} commands skipped due to parse errors)") |> ignore
        let mutable successes = 0
        let mutable failures = 0
        for r in response.Results do
            if r.Success then successes <- successes + 1
            else
                failures <- failures + 1
                sb.AppendLine($"  [{r.Index}] FAILED: {r.Message}") |> ignore
        sb.AppendLine($"  Results: {successes} succeeded, {failures} failed") |> ignore
        sb.ToString()

    /// <summary>Parses a JSON array of simulation commands and submits them as a single batch gRPC call. Supports add_body, apply_force, apply_impulse, step, play, pause, set_gravity, remove_body, clear_forces, and reset command types.</summary>
    [<McpServerTool; Description("Submit multiple simulation commands in a single batch. Commands is a JSON array where each element has a 'type' field (add_body, apply_force, apply_impulse, step, play, pause, set_gravity, remove_body, clear_forces, reset) and corresponding parameters.")>]
    static member batch_commands(conn: GrpcConnection, [<Description("JSON array of commands")>] commands: string) : Task<string> =
        task {
            try
                let doc = JsonDocument.Parse(commands)
                let batch = BatchSimulationRequest()
                let mutable parseErrors = 0
                for elem in doc.RootElement.EnumerateArray() do
                    match parseSimCommand elem with
                    | Some cmd -> batch.Commands.Add(cmd)
                    | None -> parseErrors <- parseErrors + 1
                let! response = conn.SendBatchCommand(batch)
                return formatBatchResponse response parseErrors "commands"
            with ex ->
                return $"Error: {ex.Message}"
        }

    /// <summary>Parses a JSON array of view commands and submits them as a single batch gRPC call. Supports set_camera, set_zoom, and toggle_wireframe command types.</summary>
    [<McpServerTool; Description("Submit multiple view commands in a single batch. Commands is a JSON array where each element has a 'type' field (set_camera, set_zoom, toggle_wireframe) and corresponding parameters.")>]
    static member batch_view_commands(conn: GrpcConnection, [<Description("JSON array of view commands")>] commands: string) : Task<string> =
        task {
            try
                let doc = JsonDocument.Parse(commands)
                let batch = BatchViewRequest()
                let mutable parseErrors = 0
                for elem in doc.RootElement.EnumerateArray() do
                    match parseViewCommand elem with
                    | Some cmd -> batch.Commands.Add(cmd)
                    | None -> parseErrors <- parseErrors + 1
                let! response = conn.SendBatchViewCommand(batch)
                return formatBatchResponse response parseErrors "view commands"
            with ex ->
                return $"Error: {ex.Message}"
        }
