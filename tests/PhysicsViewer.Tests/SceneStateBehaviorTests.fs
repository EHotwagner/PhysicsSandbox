module PhysicsViewer.Tests.SceneStateBehaviorTests

open Xunit
open PhysicsViewer.SceneManager

// ---------------------------------------------------------------------------
// SceneState initial values
// ---------------------------------------------------------------------------

[<Fact>]
let ``create returns state with zero simulation time`` () =
    let state = create ()
    Assert.Equal(0.0, simulationTime state)

[<Fact>]
let ``create returns state with running false`` () =
    let state = create ()
    Assert.False(isRunning state)

[<Fact>]
let ``create returns state with wireframe false`` () =
    let state = create ()
    Assert.False(isWireframe state)

// ---------------------------------------------------------------------------
// T038: SceneManager narration tests
// ---------------------------------------------------------------------------

[<Fact>]
let ``applyNarration sets NarrationText`` () =
    let state = create () |> applyNarration "Hello, world!"
    Assert.Equal(Some "Hello, world!", narrationText state)

[<Fact>]
let ``applyNarration with empty string clears NarrationText`` () =
    let state =
        create ()
        |> applyNarration "Some narration"
        |> applyNarration ""
    Assert.Equal(None, narrationText state)
