module PhysicsSimulation.Tests.StateDecompositionTests

open System.Collections.Generic
open System.Threading.Tasks
open Xunit
open PhysicsSandbox.Shared.Contracts
open CommonTestBuilders

module MR = PhysicsServer.Hub.MessageRouter

// ─── Tests ──────────────────────────────────────────────────────────────────

[<Fact>]
let ``T028: buildTickState includes only dynamic bodies`` () =
    task {
        let router = MR.create ()
        let receivedTicks = List<TickState>()

        let _sub =
            MR.subscribe router (fun ts ->
                receivedTicks.Add(ts)
                Task.CompletedTask)

        let dyn1 = makeBody "dyn1" false
        let dyn2 = makeBody "dyn2" false
        let stat1 = makeBody "stat1" true
        let state = makeState [ dyn1; dyn2; stat1 ]

        do! MR.publishState router state

        Assert.Equal(1, receivedTicks.Count)
        let tick = receivedTicks.[0]
        // Only the 2 dynamic bodies should appear in the TickState
        Assert.Equal(2, tick.Bodies.Count)
        let ids = tick.Bodies |> Seq.map (fun bp -> bp.Id) |> Set.ofSeq
        Assert.Contains("dyn1", ids)
        Assert.Contains("dyn2", ids)
        Assert.DoesNotContain("stat1", ids)
    }

[<Fact>]
let ``T029: detectPropertyEvents emits body_created for new bodies`` () =
    task {
        let router = MR.create ()
        let receivedProps = List<PropertyEvent>()

        let _sub =
            MR.subscribeProperties router (fun pe ->
                receivedProps.Add(pe)
                Task.CompletedTask)

        let body = makeBody "sphere1" false
        let state = makeState [ body ]

        do! MR.publishState router state

        Assert.Equal(1, receivedProps.Count)
        let evt = receivedProps.[0]
        Assert.NotNull(evt.BodyCreated)
        Assert.Equal("sphere1", evt.BodyCreated.Id)
        // Verify semi-static fields are populated
        Assert.NotNull(evt.BodyCreated.Shape)
        Assert.NotNull(evt.BodyCreated.Color)
        Assert.Equal(1.0, evt.BodyCreated.Mass)
        Assert.Equal(BodyMotionType.Dynamic, evt.BodyCreated.MotionType)
        Assert.False(evt.BodyCreated.IsStatic)
    }

[<Fact>]
let ``T030: detectPropertyEvents emits body_removed`` () =
    task {
        let router = MR.create ()
        let receivedProps = List<PropertyEvent>()

        let _sub =
            MR.subscribeProperties router (fun pe ->
                receivedProps.Add(pe)
                Task.CompletedTask)

        // First state: body "a" is present
        let bodyA = makeBody "a" false
        let state1 = makeState [ bodyA ]
        do! MR.publishState router state1

        // Clear events from the first publish
        receivedProps.Clear()

        // Second state: body "a" is gone
        let state2 = makeState []
        do! MR.publishState router state2

        Assert.Equal(1, receivedProps.Count)
        let evt = receivedProps.[0]
        Assert.Equal("a", evt.BodyRemoved)
    }

[<Fact>]
let ``T031: detectPropertyEvents emits body_updated on property change`` () =
    task {
        let router = MR.create ()
        let receivedProps = List<PropertyEvent>()

        let _sub =
            MR.subscribeProperties router (fun pe ->
                receivedProps.Add(pe)
                Task.CompletedTask)

        // First state: body with red color
        let body1 = makeBody "b1" false
        let state1 = makeState [ body1 ]
        do! MR.publishState router state1

        receivedProps.Clear()

        // Second state: same body with green color
        let body2 = makeBody "b1" false
        body2.Color <- Color(R = 0.0, G = 1.0, B = 0.0, A = 1.0)
        let state2 = makeState [ body2 ]
        do! MR.publishState router state2

        Assert.Equal(1, receivedProps.Count)
        let evt = receivedProps.[0]
        Assert.NotNull(evt.BodyUpdated)
        Assert.Equal("b1", evt.BodyUpdated.Id)
        // Verify the updated color is green
        Assert.Equal(0.0, evt.BodyUpdated.Color.R)
        Assert.Equal(1.0, evt.BodyUpdated.Color.G)
    }

[<Fact>]
let ``T032: no PropertyEvent when no changes`` () =
    task {
        let router = MR.create ()
        let receivedProps = List<PropertyEvent>()

        let _sub =
            MR.subscribeProperties router (fun pe ->
                receivedProps.Add(pe)
                Task.CompletedTask)

        // First state: body present
        let body = makeBody "c1" false
        let state1 = makeState [ body ]
        do! MR.publishState router state1

        // body_created fires on first publish
        Assert.Equal(1, receivedProps.Count)
        receivedProps.Clear()

        // Second state: identical body (same properties)
        let body2 = makeBody "c1" false
        let state2 = makeState [ body2 ]
        do! MR.publishState router state2

        // No property events should fire
        Assert.Empty(receivedProps)
    }

[<Fact>]
let ``T033: static body pose in BodyProperties`` () =
    task {
        let router = MR.create ()
        let receivedProps = List<PropertyEvent>()

        let _sub =
            MR.subscribeProperties router (fun pe ->
                receivedProps.Add(pe)
                Task.CompletedTask)

        let staticBody = makeBody "floor" true
        staticBody.Position <- Vec3(X = 10.0, Y = -1.0, Z = 5.0)
        staticBody.Orientation <- Vec4(X = 0.0, Y = 0.707, Z = 0.0, W = 0.707)
        let state = makeState [ staticBody ]

        do! MR.publishState router state

        Assert.Equal(1, receivedProps.Count)
        let evt = receivedProps.[0]
        Assert.NotNull(evt.BodyCreated)
        Assert.Equal("floor", evt.BodyCreated.Id)
        Assert.True(evt.BodyCreated.IsStatic)
        // Verify position is included in the BodyProperties
        Assert.Equal(10.0, evt.BodyCreated.Position.X)
        Assert.Equal(-1.0, evt.BodyCreated.Position.Y)
        Assert.Equal(5.0, evt.BodyCreated.Position.Z)
        // Verify orientation is included
        Assert.Equal(0.707, evt.BodyCreated.Orientation.Y, 3)
        Assert.Equal(0.707, evt.BodyCreated.Orientation.W, 3)
    }

// ─── Phase 6: Constraints and Registered Shapes via Property Channel ────────

[<Fact>]
let ``T084: buildTickState does not include constraints or registered shapes`` () =
    task {
        let router = MR.create ()
        let receivedTicks = List<TickState>()

        let _sub =
            MR.subscribe router (fun ts ->
                receivedTicks.Add(ts)
                Task.CompletedTask)

        // Build a state with bodies, constraints, and registered shapes
        let state = makeState [ makeBody "b1" false ]
        let cs = ConstraintState(Id = "constraint-1", BodyA = "b1", BodyB = "b1")
        state.Constraints.Add(cs)
        let rs = RegisteredShapeState(ShapeHandle = "my-shape")
        rs.Shape <- Shape(Sphere = Sphere(Radius = 1.0))
        state.RegisteredShapes.Add(rs)

        do! MR.publishState router state

        Assert.Equal(1, receivedTicks.Count)
        let tick = receivedTicks.[0]
        // TickState proto does not have constraints or registered shapes fields
        // It only contains Bodies, Time, Running, TickMs, SerializeMs, QueryResponses
        Assert.Equal(1, tick.Bodies.Count)
        Assert.Equal("b1", tick.Bodies.[0].Id)
        // Verify the tick message is lean — no constraint/shape data
        // (TickState proto literally doesn't have these fields, so this is verified by the type system)
    }

[<Fact>]
let ``T085: PropertyEvent constraints_snapshot emitted on constraint add`` () =
    task {
        let router = MR.create ()
        let receivedProps = List<PropertyEvent>()

        let _sub =
            MR.subscribeProperties router (fun pe ->
                receivedProps.Add(pe)
                Task.CompletedTask)

        // First state: body with no constraints
        let state1 = makeState [ makeBody "ca" false ]
        do! MR.publishState router state1

        receivedProps.Clear()

        // Second state: same body + a constraint
        let state2 = makeState [ makeBody "ca" false ]
        let cs = ConstraintState(Id = "c-1", BodyA = "ca", BodyB = "ca")
        state2.Constraints.Add(cs)
        do! MR.publishState router state2

        // Should have a ConstraintsSnapshot event
        let constraintEvts =
            receivedProps
            |> Seq.filter (fun e -> e.EventCase = PropertyEvent.EventOneofCase.ConstraintsSnapshot)
            |> Seq.toList

        Assert.True(constraintEvts.Length >= 1, $"Expected ConstraintsSnapshot event, got {constraintEvts.Length}")
        let snap = constraintEvts.[0].ConstraintsSnapshot
        Assert.Equal(1, snap.Constraints.Count)
        Assert.Equal("c-1", snap.Constraints.[0].Id)
    }

[<Fact>]
let ``T085b: PropertyEvent constraints_snapshot emitted on constraint remove`` () =
    task {
        let router = MR.create ()
        let receivedProps = List<PropertyEvent>()

        let _sub =
            MR.subscribeProperties router (fun pe ->
                receivedProps.Add(pe)
                Task.CompletedTask)

        // State with 1 constraint
        let state1 = makeState [ makeBody "ca" false ]
        let cs = ConstraintState(Id = "c-1", BodyA = "ca", BodyB = "ca")
        state1.Constraints.Add(cs)
        do! MR.publishState router state1

        receivedProps.Clear()

        // State with no constraints (removed)
        let state2 = makeState [ makeBody "ca" false ]
        do! MR.publishState router state2

        let constraintEvts =
            receivedProps
            |> Seq.filter (fun e -> e.EventCase = PropertyEvent.EventOneofCase.ConstraintsSnapshot)
            |> Seq.toList

        Assert.True(constraintEvts.Length >= 1)
        let snap = constraintEvts.[0].ConstraintsSnapshot
        Assert.Equal(0, snap.Constraints.Count)
    }

[<Fact>]
let ``T086: PropertyEvent registered_shapes_snapshot emitted on shape register`` () =
    task {
        let router = MR.create ()
        let receivedProps = List<PropertyEvent>()

        let _sub =
            MR.subscribeProperties router (fun pe ->
                receivedProps.Add(pe)
                Task.CompletedTask)

        // First state: no registered shapes
        let state1 = makeState [ makeBody "sa" false ]
        do! MR.publishState router state1

        receivedProps.Clear()

        // Second state: one registered shape
        let state2 = makeState [ makeBody "sa" false ]
        let rs = RegisteredShapeState(ShapeHandle = "custom-hull")
        rs.Shape <- Shape(ConvexHull = ConvexHull())
        state2.RegisteredShapes.Add(rs)
        do! MR.publishState router state2

        let shapeEvts =
            receivedProps
            |> Seq.filter (fun e -> e.EventCase = PropertyEvent.EventOneofCase.RegisteredShapesSnapshot)
            |> Seq.toList

        Assert.True(shapeEvts.Length >= 1, $"Expected RegisteredShapesSnapshot event, got {shapeEvts.Length}")
        let snap = shapeEvts.[0].RegisteredShapesSnapshot
        Assert.Equal(1, snap.RegisteredShapes.Count)
        Assert.Equal("custom-hull", snap.RegisteredShapes.[0].ShapeHandle)
    }

[<Fact>]
let ``T086b: PropertyEvent registered_shapes_snapshot emitted on shape unregister`` () =
    task {
        let router = MR.create ()
        let receivedProps = List<PropertyEvent>()

        let _sub =
            MR.subscribeProperties router (fun pe ->
                receivedProps.Add(pe)
                Task.CompletedTask)

        // State with 1 registered shape
        let state1 = makeState [ makeBody "sa" false ]
        let rs = RegisteredShapeState(ShapeHandle = "custom-hull")
        rs.Shape <- Shape(ConvexHull = ConvexHull())
        state1.RegisteredShapes.Add(rs)
        do! MR.publishState router state1

        receivedProps.Clear()

        // State with no registered shapes
        let state2 = makeState [ makeBody "sa" false ]
        do! MR.publishState router state2

        let shapeEvts =
            receivedProps
            |> Seq.filter (fun e -> e.EventCase = PropertyEvent.EventOneofCase.RegisteredShapesSnapshot)
            |> Seq.toList

        Assert.True(shapeEvts.Length >= 1)
        let snap = shapeEvts.[0].RegisteredShapesSnapshot
        Assert.Equal(0, snap.RegisteredShapes.Count)
    }
