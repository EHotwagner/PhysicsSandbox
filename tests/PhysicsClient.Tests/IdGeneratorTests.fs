module PhysicsClient.Tests.IdGeneratorTests

open Xunit
open PhysicsClient.IdGenerator

[<Fact>]
let ``nextId generates sequential IDs per shape`` () =
    let shape = $"seq-{System.Guid.NewGuid():N}"
    let id1 = nextId shape
    let id2 = nextId shape
    Assert.Equal($"{shape}-1", id1)
    Assert.Equal($"{shape}-2", id2)

[<Fact>]
let ``nextId handles different shape kinds independently`` () =
    let s = $"kindA-{System.Guid.NewGuid():N}"
    let b = $"kindB-{System.Guid.NewGuid():N}"
    let s1 = nextId s
    let b1 = nextId b
    let s2 = nextId s
    Assert.Equal($"{s}-1", s1)
    Assert.Equal($"{b}-1", b1)
    Assert.Equal($"{s}-2", s2)

[<Fact>]
let ``reset clears all counters`` () =
    let shape = $"reset-{System.Guid.NewGuid():N}"
    let _ = nextId shape
    let _ = nextId shape
    reset ()
    let id = nextId shape
    Assert.Equal($"{shape}-1", id)

[<Fact>]
let ``nextId is thread-safe`` () =
    let shape = $"threadsafe-{System.Guid.NewGuid():N}"
    let count = 100
    let ids = System.Collections.Concurrent.ConcurrentBag<string>()
    System.Threading.Tasks.Parallel.For(0, count, fun _ ->
        ids.Add(nextId shape)
    ) |> ignore
    let unique = ids |> Seq.distinct |> Seq.length
    Assert.Equal(count, unique)
    Assert.Equal(count, ids.Count)
