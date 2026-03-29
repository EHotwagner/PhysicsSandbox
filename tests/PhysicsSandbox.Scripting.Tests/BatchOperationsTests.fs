module PhysicsSandbox.Scripting.Tests.BatchOperationsTests

open Xunit
open PhysicsSandbox.Scripting.BatchOperations

[<Fact>]
let ``BatchResult with all successes has zero failures`` () =
    let result = { Succeeded = 5; Failed = [] }
    Assert.Equal(5, result.Succeeded)
    Assert.Empty(result.Failed)

[<Fact>]
let ``BatchResult with failures records index and message`` () =
    let result = { Succeeded = 3; Failed = [ (2, "Body 'sphere-1' already exists"); (4, "Invalid shape") ] }
    Assert.Equal(3, result.Succeeded)
    Assert.Equal(2, result.Failed.Length)
    let (idx, msg) = result.Failed.[0]
    Assert.Equal(2, idx)
    Assert.Contains("sphere-1", msg)

[<Fact>]
let ``BatchResult empty batch has zero succeeded and no failures`` () =
    let result = { Succeeded = 0; Failed = [] }
    Assert.Equal(0, result.Succeeded)
    Assert.Empty(result.Failed)
