module PhysicsSandbox.Mcp.Recording.RecordingEngine

open System
open PhysicsSandbox.Mcp.Recording.Types

[<Sealed>]
type RecordingEngine =
    member Start: ?label:string * ?timeLimitMinutes:int * ?sizeLimitBytes:int64 -> unit
    member Stop: unit -> unit
    member IsRecording: bool
    member ActiveSession: RecordingSession option
    member OnStateReceived: PhysicsSandbox.Shared.Contracts.SimulationState -> unit
    member OnCommandReceived: PhysicsSandbox.Shared.Contracts.CommandEvent -> unit
    interface IDisposable

val create: unit -> RecordingEngine
