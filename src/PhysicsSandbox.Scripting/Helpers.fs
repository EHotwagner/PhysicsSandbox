module PhysicsSandbox.Scripting.Helpers

let ok r = r |> Result.defaultWith (fun e -> failwith e)

let sleep (ms: int) = System.Threading.Thread.Sleep(ms)

let timed (label: string) (f: unit -> 'a) : 'a =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let result = f ()
    sw.Stop()
    printfn "  [TIME] %s: %d ms" label sw.ElapsedMilliseconds
    result
