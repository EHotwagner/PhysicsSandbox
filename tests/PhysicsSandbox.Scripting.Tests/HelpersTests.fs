module PhysicsSandbox.Scripting.Tests.HelpersTests

open Xunit
open PhysicsSandbox.Scripting.Helpers

[<Fact>]
let ``ok returns value on Ok result`` () =
    let result = Ok 42
    Assert.Equal(42, ok result)

[<Fact>]
let ``ok throws on Error result`` () =
    let result : Result<int, string> = Error "test error"
    let ex = Assert.Throws<System.Exception>(fun () -> ok result |> ignore)
    Assert.Contains("test error", ex.Message)

[<Fact>]
let ``timed returns correct result`` () =
    let result = timed "test" (fun () -> 42)
    Assert.Equal(42, result)

[<Fact>]
let ``timed returns correct result for string`` () =
    let result = timed "test" (fun () -> "hello")
    Assert.Equal("hello", result)
