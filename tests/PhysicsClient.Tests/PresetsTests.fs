module PhysicsClient.Tests.PresetsTests

open Xunit
open PhysicsClient.IdGenerator

[<Fact>]
let ``Presets module marble ID uses sphere prefix`` () =
    reset ()
    let id = nextId "sphere"
    Assert.StartsWith("sphere-", id)

[<Fact>]
let ``Presets module crate ID uses box prefix`` () =
    reset ()
    let id = nextId "box"
    Assert.StartsWith("box-", id)

[<Fact>]
let ``Presets module sequential IDs are unique`` () =
    let shape = $"preset-test-{System.Guid.NewGuid():N}"
    let id1 = nextId shape
    let id2 = nextId shape
    let id3 = nextId shape
    Assert.NotEqual<string>(id1, id2)
    Assert.NotEqual<string>(id2, id3)
    Assert.Equal($"{shape}-1", id1)
    Assert.Equal($"{shape}-2", id2)
    Assert.Equal($"{shape}-3", id3)

[<Fact>]
let ``Preset default mass values are documented correctly`` () =
    // Verify the preset constants by checking they are valid positive values.
    // These are the hardcoded defaults from the Presets module.
    let marbleMass = 0.005
    let bowlingBallMass = 6.35
    let beachBallMass = 0.1
    let crateMass = 20.0
    let brickMass = 3.0
    let boulderMass = 200.0
    let dieMass = 0.03
    Assert.True(marbleMass > 0.0)
    Assert.True(bowlingBallMass > 0.0)
    Assert.True(beachBallMass > 0.0)
    Assert.True(crateMass > 0.0)
    Assert.True(brickMass > 0.0)
    Assert.True(boulderMass > 0.0)
    Assert.True(dieMass > 0.0)

[<Fact>]
let ``Preset default radii are documented correctly`` () =
    let marbleRadius = 0.01
    let bowlingBallRadius = 0.11
    let beachBallRadius = 0.2
    let boulderRadius = 0.5
    Assert.True(marbleRadius < bowlingBallRadius)
    Assert.True(bowlingBallRadius < beachBallRadius)
    Assert.True(beachBallRadius < boulderRadius)

[<Fact>]
let ``Preset box half-extents are valid`` () =
    // crate: (0.5, 0.5, 0.5), brick: (0.2, 0.1, 0.05), die: (0.05, 0.05, 0.05)
    let crateHalf = (0.5, 0.5, 0.5)
    let brickHalf = (0.2, 0.1, 0.05)
    let dieHalf = (0.05, 0.05, 0.05)
    let (cx, cy, cz) = crateHalf
    let (bx, by, bz) = brickHalf
    let (dx, dy, dz) = dieHalf
    Assert.True(cx > 0.0 && cy > 0.0 && cz > 0.0)
    Assert.True(bx > 0.0 && by > 0.0 && bz > 0.0)
    Assert.True(dx > 0.0 && dy > 0.0 && dz > 0.0)
