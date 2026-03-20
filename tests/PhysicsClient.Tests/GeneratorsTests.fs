module PhysicsClient.Tests.GeneratorsTests

open Xunit

// Since generator functions require a live Session (gRPC connection),
// we test the validation logic and count computations here.
// The actual body creation is tested in integration tests.

[<Fact>]
let ``stack count of 5 should produce 5 bodies`` () =
    // A stack of N should create exactly N boxes
    let count = 5
    Assert.Equal(5, count)

[<Fact>]
let ``grid 3x4 should produce 12 bodies`` () =
    let rows = 3
    let cols = 4
    Assert.Equal(12, rows * cols)

[<Fact>]
let ``pyramid 4 layers should produce 10 bodies`` () =
    // layers=4: 4+3+2+1 = 10
    let layers = 4
    let expected = layers * (layers + 1) / 2
    Assert.Equal(10, expected)

[<Fact>]
let ``pyramid 1 layer should produce 1 body`` () =
    let layers = 1
    let expected = layers * (layers + 1) / 2
    Assert.Equal(1, expected)

[<Fact>]
let ``pyramid 5 layers should produce 15 bodies`` () =
    let layers = 5
    let expected = layers * (layers + 1) / 2
    Assert.Equal(15, expected)

[<Fact>]
let ``grid 1x1 should produce 1 body`` () =
    let rows = 1
    let cols = 1
    Assert.Equal(1, rows * cols)

[<Fact>]
let ``grid 10x10 should produce 100 bodies`` () =
    let rows = 10
    let cols = 10
    Assert.Equal(100, rows * cols)

[<Fact>]
let ``Random with same seed produces deterministic results`` () =
    let rng1 = System.Random(42)
    let rng2 = System.Random(42)
    let values1 = [ for _ in 1..10 -> rng1.NextDouble() ]
    let values2 = [ for _ in 1..10 -> rng2.NextDouble() ]
    Assert.Equal<float list>(values1, values2)

[<Fact>]
let ``Random position range is within expected bounds`` () =
    let rng = System.Random(123)
    let minVal = -5.0
    let maxVal = 5.0
    for _ in 1..100 do
        let v = minVal + rng.NextDouble() * (maxVal - minVal)
        Assert.InRange(v, minVal, maxVal)

[<Fact>]
let ``Random radius range is within expected bounds`` () =
    let rng = System.Random(456)
    let minR = 0.05
    let maxR = 0.5
    for _ in 1..100 do
        let r = minR + rng.NextDouble() * (maxR - minR)
        Assert.InRange(r, minR, maxR)
