module PhysicsViewer.DebugRenderer

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Games
open Stride.Graphics
open Stride.Rendering
open Stride.CommunityToolkit.Engine
open Stride.CommunityToolkit.Games
open Stride.CommunityToolkit.Rendering.ProceduralModels
open PhysicsSandbox.Shared.Contracts

type StrideColor = Stride.Core.Mathematics.Color

/// Debug renderer state tracks wireframe entities for bodies and constraint lines.
type DebugState =
    { Enabled: bool
      /// Wireframe entities for each body (body ID -> entity list for compound children).
      WireframeEntities: Map<string, Entity list>
      /// Line entities for each constraint (constraint ID -> entity).
      ConstraintEntities: Map<string, Entity> }

let create () =
    { Enabled = false
      WireframeEntities = Map.empty
      ConstraintEntities = Map.empty }

let private protoVec3ToStride (v: Vec3) =
    if isNull v then Vector3.Zero
    else Vector3(float32 v.X, float32 v.Y, float32 v.Z)

let private protoQuatToStride (v: Vec4) =
    if isNull v then Quaternion.Identity
    else Quaternion(float32 v.X, float32 v.Y, float32 v.Z, float32 v.W)

/// Create a wireframe entity from custom mesh wireframe data (LineList).
let private createCustomWireframe (game: Game) (scene: Scene) (meshData: ShapeGeometry.CustomMeshData) (pos: Vector3) (rot: Quaternion) =
    if meshData.WireframePositions.Length = 0 || meshData.WireframeIndices.Length = 0 then
        None
    else
        let wireColor = StrideColor(0uy, 255uy, 0uy, 128uy)
        let material = game.CreateFlatMaterial(System.Nullable<StrideColor>(wireColor))
        let gd = game.GraphicsDevice
        let vertices =
            meshData.WireframePositions
            |> Array.map (fun v -> VertexPositionNormalColor(v, Vector3.UnitY, wireColor))
        let vertexBuffer = Stride.Graphics.Buffer.Vertex.New<VertexPositionNormalColor>(gd, vertices)
        let indexBuffer = Stride.Graphics.Buffer.Index.New<int>(gd, meshData.WireframeIndices)
        let meshDraw =
            MeshDraw(
                PrimitiveType = PrimitiveType.LineList,
                DrawCount = meshData.WireframeIndices.Length,
                IndexBuffer = IndexBufferBinding(indexBuffer, true, meshData.WireframeIndices.Length),
                VertexBuffers = [|
                    VertexBufferBinding(vertexBuffer, VertexPositionNormalColor.Layout, vertices.Length)
                |])
        let mesh = Mesh(Draw = meshDraw)
        let model = Model()
        model.Meshes.Add(mesh)
        model.Materials.Add(MaterialInstance(material))
        let entity = Entity()
        let mc = entity.GetOrCreate<ModelComponent>()
        mc.Model <- model
        entity.Transform.Position <- pos
        entity.Transform.Rotation <- rot
        entity.Scene <- scene
        Some entity

/// Create a single wireframe primitive entity.
let private createPrimitiveWireframe (game: Game) (scene: Scene) (shape: Shape) (pos: Vector3) (rot: Quaternion) =
    let primType = ShapeGeometry.primitiveType shape
    let wireColor = StrideColor(0uy, 255uy, 0uy, 128uy)
    let material = game.CreateFlatMaterial(System.Nullable<StrideColor>(wireColor))
    let size = ShapeGeometry.shapeSize shape
    let options = Primitive3DEntityOptions(Material = material, Size = size)
    let entity = game.Create3DPrimitive(primType, options)
    entity.Transform.Position <- pos
    entity.Transform.Rotation <- rot
    entity.Scene <- scene
    entity

/// Create a wireframe entity for any shape (custom or primitive).
let private createShapeWireframe (game: Game) (scene: Scene) (shape: Shape) (pos: Vector3) (rot: Quaternion) =
    if ShapeGeometry.isCustomShape shape then
        let color = StrideColor(0uy, 255uy, 0uy, 128uy)
        match ShapeGeometry.buildCustomMesh shape color with
        | Some meshData ->
            match createCustomWireframe game scene meshData pos rot with
            | Some entity -> entity
            | None -> createPrimitiveWireframe game scene shape pos rot
        | None -> createPrimitiveWireframe game scene shape pos rot
    else
        createPrimitiveWireframe game scene shape pos rot

/// Create wireframe entities for a body. Returns a list (>1 for compound shapes).
let private createWireframeEntities (game: Game) (scene: Scene) (body: Body) =
    let bodyPos = protoVec3ToStride body.Position
    let bodyRot = protoQuatToStride body.Orientation

    if not (isNull body.Shape) && body.Shape.ShapeCase = Shape.ShapeOneofCase.Compound
       && not (isNull body.Shape.Compound) && body.Shape.Compound.Children.Count > 0 then
        // Compound: render each child shape individually
        [ for child in body.Shape.Compound.Children do
            if not (isNull child.Shape) then
                let childLocalPos = protoVec3ToStride child.LocalPosition
                let childLocalRot = protoQuatToStride child.LocalOrientation
                let mutable worldPos = Vector3.Zero
                Vector3.Transform(&childLocalPos, &bodyRot, &worldPos)
                Vector3.Add(&worldPos, &bodyPos, &worldPos)
                let mutable worldRot = Quaternion.Identity
                Quaternion.Multiply(&bodyRot, &childLocalRot, &worldRot)
                yield createShapeWireframe game scene child.Shape worldPos worldRot ]
    else
        [ createShapeWireframe game scene body.Shape bodyPos bodyRot ]

/// Color for constraint type visualization.
let private constraintColor (cs: ConstraintState) =
    if isNull cs || isNull cs.Type then StrideColor.White
    else
        match cs.Type.ConstraintCase with
        | ConstraintType.ConstraintOneofCase.BallSocket -> StrideColor.Cyan
        | ConstraintType.ConstraintOneofCase.Hinge -> StrideColor.Yellow
        | ConstraintType.ConstraintOneofCase.Weld -> StrideColor.Red
        | ConstraintType.ConstraintOneofCase.DistanceLimit -> StrideColor.Green
        | ConstraintType.ConstraintOneofCase.DistanceSpring -> StrideColor(0uy, 255uy, 128uy, 255uy)
        | ConstraintType.ConstraintOneofCase.SwingLimit -> StrideColor.Orange
        | ConstraintType.ConstraintOneofCase.TwistLimit -> StrideColor.Purple
        | ConstraintType.ConstraintOneofCase.LinearAxisMotor -> StrideColor(255uy, 128uy, 0uy, 255uy)
        | ConstraintType.ConstraintOneofCase.AngularMotor -> StrideColor(128uy, 0uy, 255uy, 255uy)
        | ConstraintType.ConstraintOneofCase.PointOnLine -> StrideColor(255uy, 255uy, 0uy, 255uy)
        | _ -> StrideColor.White

/// Create a thin cylinder between two positions to visualize a constraint connection.
let private createConstraintLine (game: Game) (scene: Scene) (posA: Vector3) (posB: Vector3) (color: StrideColor) =
    let material = game.CreateFlatMaterial(System.Nullable<StrideColor>(color))
    let mutable mid = Vector3.Zero
    let mutable diff = Vector3.Zero
    Vector3.Add(&posA, &posB, &mid)
    Vector3.Multiply(&mid, 0.5f, &mid)
    Vector3.Subtract(&posB, &posA, &diff)
    let dist = diff.Length()
    if dist < 0.001f then None
    else
        let size = System.Nullable(Vector3(0.05f, dist, 0.05f))
        let options = Primitive3DEntityOptions(Material = material, Size = size)
        let entity = game.Create3DPrimitive(PrimitiveModelType.Cylinder, options)
        entity.Transform.Position <- mid
        let dir = Vector3.Normalize(diff)
        let up = Vector3.UnitY
        if abs (Vector3.Dot(dir, up)) < 0.999f then
            let mutable rot = Quaternion.Identity
            Quaternion.BetweenDirections(&up, &dir, &rot)
            entity.Transform.Rotation <- rot
        entity.Scene <- scene
        Some entity

let private removeAllEntities (entities: Map<string, Entity list>) =
    for kvp in entities do
        for e in kvp.Value do
            e.Scene <- null
    Map.empty

let updateShapes (game: Game) (scene: Scene) (state: DebugState) (simState: SimulationState) =
    if not state.Enabled || isNull simState || isNull simState.Bodies then state
    else

    let incomingIds = simState.Bodies |> Seq.map (fun b -> b.Id) |> Set.ofSeq

    let entitiesToRemove =
        state.WireframeEntities |> Map.filter (fun id _ -> not (Set.contains id incomingIds))
    for kvp in entitiesToRemove do
        for e in kvp.Value do
            e.Scene <- null

    let mutable updated =
        state.WireframeEntities |> Map.filter (fun id _ -> Set.contains id incomingIds)

    for body in simState.Bodies do
        match Map.tryFind body.Id updated with
        | Some entities ->
            match entities with
            | [ single ] ->
                single.Transform.Position <- protoVec3ToStride body.Position
                single.Transform.Rotation <- protoQuatToStride body.Orientation
            | _ ->
                for e in entities do e.Scene <- null
                let newEntities = createWireframeEntities game scene body
                updated <- Map.add body.Id newEntities updated
        | None ->
            let entities = createWireframeEntities game scene body
            updated <- Map.add body.Id entities updated

    { state with WireframeEntities = updated }

let updateConstraints (game: Game) (scene: Scene) (state: DebugState) (simState: SimulationState) =
    if not state.Enabled || isNull simState then state
    else

    for kvp in state.ConstraintEntities do
        kvp.Value.Scene <- null

    let bodyPositions =
        if isNull simState.Bodies then Map.empty
        else
            simState.Bodies
            |> Seq.map (fun b -> b.Id, protoVec3ToStride b.Position)
            |> Map.ofSeq

    let mutable constraintEnts = Map.empty

    if not (isNull simState.Constraints) then
        for cs in simState.Constraints do
            match Map.tryFind cs.BodyA bodyPositions, Map.tryFind cs.BodyB bodyPositions with
            | Some posA, Some posB ->
                let color = constraintColor cs
                match createConstraintLine game scene posA posB color with
                | Some entity ->
                    constraintEnts <- Map.add cs.Id entity constraintEnts
                | None -> ()
            | _ -> ()

    { state with ConstraintEntities = constraintEnts }

let setEnabled (enabled: bool) (state: DebugState) =
    if enabled = state.Enabled then state
    else
        if not enabled then
            let wireframes = removeAllEntities state.WireframeEntities
            let constraints =
                for kvp in state.ConstraintEntities do
                    kvp.Value.Scene <- null
                Map.empty
            { state with Enabled = false; WireframeEntities = wireframes; ConstraintEntities = constraints }
        else
            { state with Enabled = true }

let isEnabled (state: DebugState) = state.Enabled
