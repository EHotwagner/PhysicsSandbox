module PhysicsSandbox.Scripting.Helpers

val ok : Result<'a, string> -> 'a
val sleep : int -> unit
val timed : string -> (unit -> 'a) -> 'a
