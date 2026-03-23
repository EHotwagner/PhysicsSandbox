module PhysicsSandbox.Mcp.Recording.SessionStore

open System
open System.IO
open System.Text.Json
open PhysicsSandbox.Mcp.Recording.Types

let private recordingsDir =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "PhysicsSandbox", "recordings")

let getRecordingsDir () = recordingsDir

let private sessionDir (sessionId: string) =
    Path.Combine(recordingsDir, sessionId)

let private sessionJsonPath (sessionId: string) =
    Path.Combine(sessionDir sessionId, "session.json")

let private statusToString = function
    | SessionStatus.Recording -> "Recording"
    | SessionStatus.Completed -> "Completed"
    | SessionStatus.Failed -> "Failed"

let private statusFromString = function
    | "Recording" -> SessionStatus.Recording
    | "Completed" -> SessionStatus.Completed
    | "Failed" -> SessionStatus.Failed
    | _ -> SessionStatus.Failed

let private clampLabel (label: string) =
    let l = if String.IsNullOrWhiteSpace label then $"""Session {DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}""" else label
    if l.Length > 200 then l.Substring(0, 200) else l

let private clampTimeLimit (minutes: int) =
    Math.Clamp(minutes, 1, 1440)

let private clampSizeLimit (bytes: int64) =
    Math.Clamp(bytes, 1_000_000L, 10_000_000_000L)

let private writeSession (session: RecordingSession) =
    let dir = sessionDir session.Id
    Directory.CreateDirectory(dir) |> ignore
    use stream = new MemoryStream()
    use writer = new Utf8JsonWriter(stream, JsonWriterOptions(Indented = true))
    writer.WriteStartObject()
    writer.WriteString("Id", session.Id)
    writer.WriteString("Label", session.Label)
    writer.WriteString("Status", statusToString session.Status)
    writer.WriteString("StartTime", session.StartTime.ToString("o"))
    writer.WriteString("EndTime", match session.EndTime with Some dt -> dt.ToString("o") | None -> "")
    writer.WriteNumber("TimeLimitMinutes", session.TimeLimitMinutes)
    writer.WriteNumber("SizeLimitBytes", session.SizeLimitBytes)
    writer.WriteNumber("CurrentSizeBytes", session.CurrentSizeBytes)
    writer.WriteNumber("ChunkCount", session.ChunkCount)
    writer.WriteNumber("SnapshotCount", session.SnapshotCount)
    writer.WriteNumber("EventCount", session.EventCount)
    writer.WriteEndObject()
    writer.Flush()
    File.WriteAllBytes(sessionJsonPath session.Id, stream.ToArray())

let private readSession (sessionId: string) : RecordingSession option =
    let path = sessionJsonPath sessionId
    try
        if not (File.Exists path) then None
        else
            let json = File.ReadAllText(path)
            use doc = JsonDocument.Parse(json)
            let root = doc.RootElement
            let getString (name: string) def =
                let mutable el = JsonElement()
                if root.TryGetProperty(name, &el) then el.GetString() else def
            let getInt (name: string) def =
                let mutable el = JsonElement()
                if root.TryGetProperty(name, &el) then el.GetInt32() else def
            let getInt64 (name: string) def =
                let mutable el = JsonElement()
                if root.TryGetProperty(name, &el) then el.GetInt64() else def
            let endTimeStr = getString "EndTime" ""
            let endTime =
                if String.IsNullOrEmpty endTimeStr then None
                else
                    match DateTimeOffset.TryParse(endTimeStr) with
                    | true, dt -> Some dt
                    | false, _ -> None
            Some
                { Id = getString "Id" sessionId
                  Label = getString "Label" ""
                  Status = statusFromString (getString "Status" "Failed")
                  StartTime =
                      match DateTimeOffset.TryParse(getString "StartTime" "") with
                      | true, dt -> dt
                      | false, _ -> DateTimeOffset.UtcNow
                  EndTime = endTime
                  TimeLimitMinutes = getInt "TimeLimitMinutes" 60
                  SizeLimitBytes = getInt64 "SizeLimitBytes" 104_857_600L
                  CurrentSizeBytes = getInt64 "CurrentSizeBytes" 0L
                  ChunkCount = getInt "ChunkCount" 0
                  SnapshotCount = getInt64 "SnapshotCount" 0L
                  EventCount = getInt64 "EventCount" 0L }
    with _ -> None

let createSession (label: string) (timeLimitMinutes: int) (sizeLimitBytes: int64) : RecordingSession =
    let session =
        { Id = Guid.NewGuid().ToString()
          Label = clampLabel label
          Status = SessionStatus.Recording
          StartTime = DateTimeOffset.UtcNow
          EndTime = None
          TimeLimitMinutes = clampTimeLimit timeLimitMinutes
          SizeLimitBytes = clampSizeLimit sizeLimitBytes
          CurrentSizeBytes = 0L
          ChunkCount = 0
          SnapshotCount = 0L
          EventCount = 0L }
    writeSession session
    session

let loadSession (sessionId: string) : RecordingSession option =
    readSession sessionId

let updateSession (session: RecordingSession) : unit =
    try writeSession session with _ -> ()

let deleteSession (sessionId: string) : bool =
    let dir = sessionDir sessionId
    try
        if Directory.Exists dir then
            Directory.Delete(dir, true)
            true
        else
            false
    with _ -> false

let listSessions () : RecordingSession list =
    try
        if not (Directory.Exists recordingsDir) then []
        else
            Directory.GetDirectories(recordingsDir)
            |> Array.choose (fun dir ->
                let id = Path.GetFileName(dir)
                if File.Exists(sessionJsonPath id) then readSession id
                else None)
            |> Array.toList
    with _ -> []

let getActiveSession () : RecordingSession option =
    listSessions ()
    |> List.tryFind (fun s -> s.Status = SessionStatus.Recording)
