module PhysicsSimulation.ShapeConversion

open System.Numerics
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.ProtoConversions
open BepuFSharp

let toBepuMaterial (mat: ProtoMaterialProperties) : BepuFSharp.MaterialProperties =
    BepuFSharp.MaterialProperties.create
        (float32 mat.Friction)
        (float32 mat.MaxRecoveryVelocity)
        (float32 mat.SpringFrequency)
        (float32 mat.SpringDampingRatio)

let convertConstraintType (ct: ConstraintType) =
    let toSpring (s: PhysicsSandbox.Shared.Contracts.SpringSettings) =
        if isNull s then SpringConfig.create 30.0f 1.0f
        else SpringConfig.create (float32 s.Frequency) (float32 s.DampingRatio)

    let toMotor (m: MotorConfig) =
        if isNull m then { MaxForce = 1000.0f; Damping = 1.0f }
        else { MaxForce = float32 m.MaxForce; Damping = float32 m.Damping }

    let v3 (v: Vec3) = if isNull v then Vector3.Zero else toVector3 v
    let q4 (v: Vec4) = if isNull v then Quaternion.Identity else toQuaternion v

    match ct.ConstraintCase with
    | ConstraintType.ConstraintOneofCase.BallSocket ->
        let c = ct.BallSocket
        Some (ConstraintDesc.BallSocket(v3 c.LocalOffsetA, v3 c.LocalOffsetB, toSpring c.Spring))
    | ConstraintType.ConstraintOneofCase.Hinge ->
        let c = ct.Hinge
        Some (ConstraintDesc.Hinge(v3 c.LocalHingeAxisA, v3 c.LocalHingeAxisB, v3 c.LocalOffsetA, v3 c.LocalOffsetB, toSpring c.Spring))
    | ConstraintType.ConstraintOneofCase.Weld ->
        let c = ct.Weld
        Some (ConstraintDesc.Weld(v3 c.LocalOffset, q4 c.LocalOrientation, toSpring c.Spring))
    | ConstraintType.ConstraintOneofCase.DistanceLimit ->
        let c = ct.DistanceLimit
        Some (ConstraintDesc.DistanceLimit(v3 c.LocalOffsetA, v3 c.LocalOffsetB, float32 c.MinDistance, float32 c.MaxDistance, toSpring c.Spring))
    | ConstraintType.ConstraintOneofCase.DistanceSpring ->
        let c = ct.DistanceSpring
        Some (ConstraintDesc.DistanceSpring(v3 c.LocalOffsetA, v3 c.LocalOffsetB, float32 c.TargetDistance, toSpring c.Spring))
    | ConstraintType.ConstraintOneofCase.SwingLimit ->
        let c = ct.SwingLimit
        Some (ConstraintDesc.SwingLimit(v3 c.AxisLocalA, v3 c.AxisLocalB, float32 c.MaxSwingAngle, toSpring c.Spring))
    | ConstraintType.ConstraintOneofCase.TwistLimit ->
        let c = ct.TwistLimit
        Some (ConstraintDesc.TwistLimit(v3 c.LocalAxisA, v3 c.LocalAxisB, float32 c.MinAngle, float32 c.MaxAngle, toSpring c.Spring))
    | ConstraintType.ConstraintOneofCase.LinearAxisMotor ->
        let c = ct.LinearAxisMotor
        Some (ConstraintDesc.LinearAxisMotor(v3 c.LocalOffsetA, v3 c.LocalOffsetB, v3 c.LocalAxis, float32 c.TargetVelocity, toMotor c.Motor))
    | ConstraintType.ConstraintOneofCase.AngularMotor ->
        let c = ct.AngularMotor
        Some (ConstraintDesc.AngularMotor(v3 c.TargetVelocity, toMotor c.Motor))
    | ConstraintType.ConstraintOneofCase.PointOnLine ->
        let c = ct.PointOnLine
        Some (ConstraintDesc.PointOnLine(v3 c.LocalOrigin, v3 c.LocalDirection, v3 c.LocalOffset, toSpring c.Spring))
    | _ -> None

let rec convertShape (physicsWorld: PhysicsWorld) (registeredShapes: Map<string, ShapeId * Shape>) (shape: Shape) =
    match shape with
    | null -> Ok (PhysicsShape.Sphere 1.0f, Shape(Sphere = ProtoSphere(Radius = 1.0)))
    | s when s.ShapeCase = Shape.ShapeOneofCase.Sphere ->
        Ok (PhysicsShape.Sphere(float32 s.Sphere.Radius), s)
    | s when s.ShapeCase = Shape.ShapeOneofCase.Box ->
        let he = s.Box.HalfExtents
        let w, h, l =
            if he <> null then float32 he.X * 2.0f, float32 he.Y * 2.0f, float32 he.Z * 2.0f
            else 1.0f, 1.0f, 1.0f
        Ok (PhysicsShape.Box(w, h, l), s)
    | s when s.ShapeCase = Shape.ShapeOneofCase.Plane ->
        Ok (PhysicsShape.Box(1000.0f, 0.1f, 1000.0f), s)
    | s when s.ShapeCase = Shape.ShapeOneofCase.Capsule ->
        let r = float32 s.Capsule.Radius
        let len = float32 s.Capsule.Length
        if r <= 0.0f || len <= 0.0f then
            Error "Capsule radius and length must be positive"
        else
            Ok (PhysicsShape.Capsule(r, len), s)
    | s when s.ShapeCase = Shape.ShapeOneofCase.Cylinder ->
        let r = float32 s.Cylinder.Radius
        let len = float32 s.Cylinder.Length
        if r <= 0.0f || len <= 0.0f then
            Error "Cylinder radius and length must be positive"
        else
            Ok (PhysicsShape.Cylinder(r, len), s)
    | s when s.ShapeCase = Shape.ShapeOneofCase.Triangle ->
        let a = if isNull s.Triangle.A then Vector3.Zero else toVector3 s.Triangle.A
        let b = if isNull s.Triangle.B then Vector3.UnitX else toVector3 s.Triangle.B
        let c = if isNull s.Triangle.C then Vector3.UnitY else toVector3 s.Triangle.C
        Ok (PhysicsShape.Triangle(a, b, c), s)
    | s when s.ShapeCase = Shape.ShapeOneofCase.ConvexHull ->
        let points = s.ConvexHull.Points |> Seq.map toVector3 |> Seq.toArray
        if points.Length < 4 then
            Error "ConvexHull requires at least 4 points"
        else
            Ok (PhysicsShape.ConvexHull(points), s)
    | s when s.ShapeCase = Shape.ShapeOneofCase.Compound ->
        if s.Compound.Children.Count = 0 then
            Error "Compound shape requires at least 1 child"
        else
            let mutable childError = None
            let children =
                s.Compound.Children
                |> Seq.map (fun child ->
                    match childError with
                    | Some _ -> Unchecked.defaultof<BepuFSharp.CompoundChild>
                    | None ->
                        match convertShape physicsWorld registeredShapes child.Shape with
                        | Ok (childPhysShape, _) ->
                            let pos =
                                if isNull child.LocalPosition then Vector3.Zero
                                else toVector3 child.LocalPosition
                            let ori =
                                if isNull child.LocalOrientation then Quaternion.Identity
                                else toQuaternion child.LocalOrientation
                            let childShapeId = PhysicsWorld.addShape childPhysShape physicsWorld
                            { Shape = childShapeId; LocalPose = Pose.create pos ori }
                        | Error msg ->
                            childError <- Some msg
                            Unchecked.defaultof<BepuFSharp.CompoundChild>)
                |> Seq.toArray
            match childError with
            | Some msg -> Error $"Compound child error: {msg}"
            | None -> Ok (PhysicsShape.Compound(children), s)
    | s when s.ShapeCase = Shape.ShapeOneofCase.Mesh ->
        if s.Mesh.Triangles.Count = 0 then
            Error "MeshShape requires at least 1 triangle"
        else
            let triangles =
                s.Mesh.Triangles
                |> Seq.map (fun t ->
                    let a = if isNull t.A then Vector3.Zero else toVector3 t.A
                    let b = if isNull t.B then Vector3.UnitX else toVector3 t.B
                    let c = if isNull t.C then Vector3.UnitY else toVector3 t.C
                    (a, b, c))
                |> Seq.toArray
            Ok (PhysicsShape.Mesh(triangles), s)
    | s when s.ShapeCase = Shape.ShapeOneofCase.CachedRef ->
        Error "CachedShapeRef cannot be used in simulation commands"
    | s when s.ShapeCase = Shape.ShapeOneofCase.ShapeRef ->
        let handle = s.ShapeRef.ShapeHandle
        match Map.tryFind handle registeredShapes with
        | Some (_shapeId, origShape) ->
            Ok (PhysicsShape.Sphere 0.0f, origShape)
        | None ->
            Error $"Unknown shape reference: '{handle}'"
    | _ -> Ok (PhysicsShape.Sphere 1.0f, shape)
