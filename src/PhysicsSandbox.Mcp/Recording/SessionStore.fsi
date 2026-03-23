module PhysicsSandbox.Mcp.Recording.SessionStore

open PhysicsSandbox.Mcp.Recording.Types

val getRecordingsDir: unit -> string
val createSession: label:string -> timeLimitMinutes:int -> sizeLimitBytes:int64 -> RecordingSession
val loadSession: sessionId:string -> RecordingSession option
val updateSession: session:RecordingSession -> unit
val deleteSession: sessionId:string -> bool
val listSessions: unit -> RecordingSession list
val getActiveSession: unit -> RecordingSession option
