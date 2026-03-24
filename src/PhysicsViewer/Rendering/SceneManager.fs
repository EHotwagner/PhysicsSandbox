module PhysicsViewer.SceneManager

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Games
open Stride.Graphics
open Stride.Rendering
open Stride.CommunityToolkit.Engine
open Stride.CommunityToolkit.Games
open PhysicsSandbox.Shared.Contracts
module MeshResolver = PhysicsViewer.Streaming.MeshResolver

type StrideColor = Stride.Core.Mathematics.Color
type ProtoColor = PhysicsSandbox.Shared.Contracts.Color

/// Tracks the 3D scene state, mapping simulation body IDs to Stride entities.
type SceneState =
    { Bodies: Map<string, Entity>
      Placeholders: Set<string>
      SimTime: float
      SimRunning: bool
      Wireframe: bool
      DemoName: string option
      DemoDescription: string option
      NarrationText: string option }

/// Creates an empty scene state.
let create () =
    { Bodies = Map.empty
      Placeholders = Set.empty
      SimTime = 0.0
      SimRunning = false
      Wireframe = false
      DemoName = None
      DemoDescription = None
      NarrationText = None }

let private protoVec3ToStride (v: Vec3) =
    if isNull v then Vector3.Zero
    else Vector3(float32 v.X, float32 v.Y, float32 v.Z)

let private protoQuatToStride (v: Vec4) =
    if isNull v then Quaternion.Identity
    else Quaternion(float32 v.X, float32 v.Y, float32 v.Z, float32 v.W)

/// Convert proto Color to Stride Color.
let private protoColorToStride (c: ProtoColor) =
    if isNull c then None
    else
        Some (StrideColor(byte (c.R * 255.0), byte (c.G * 255.0), byte (c.B * 255.0), byte (c.A * 255.0)))

/// Determine the visual color for a body.
let private bodyColor (body: Body) (renderShape: Shape) : StrideColor =
    match protoColorToStride body.Color with
    | Some c -> c
    | None -> ShapeGeometry.defaultColor renderShape

/// Build VertexPositionNormalColor array from CustomMeshData.
let private buildVertices (meshData: ShapeGeometry.CustomMeshData) =
    let count = meshData.Positions.Length
    let verts = Array.zeroCreate<VertexPositionNormalColor> count
    for i in 0 .. count - 1 do
        verts.[i] <- VertexPositionNormalColor(meshData.Positions.[i], meshData.Normals.[i], meshData.Color)
    verts

/// Create a Stride Model from CustomMeshData.
let private createModelFromMeshData (gd: GraphicsDevice) (meshData: ShapeGeometry.CustomMeshData) (material: Material) =
    let verts = buildVertices meshData
    let vertexBuffer = Stride.Graphics.Buffer.Vertex.New<VertexPositionNormalColor>(gd, verts)
    let indexBuffer = Stride.Graphics.Buffer.Index.New<int>(gd, meshData.Indices)
    let meshDraw =
        MeshDraw(
            PrimitiveType = PrimitiveType.TriangleList,
            DrawCount = meshData.Indices.Length,
            IndexBuffer = IndexBufferBinding(indexBuffer, true, meshData.Indices.Length),
            VertexBuffers = [|
                VertexBufferBinding(vertexBuffer, VertexPositionNormalColor.Layout, verts.Length)
            |])
    let mesh = Mesh(Draw = meshDraw)
    let model = Model()
    model.Meshes.Add(mesh)
    model.Materials.Add(MaterialInstance(material))
    model

/// Returns (shape to render, isPlaceholder).
/// Resolves CachedRef via MeshResolver and ShapeRef via RegisteredShapes.
let private resolveShape (resolver: MeshResolver.MeshResolverState option) (simState: SimulationState) (body: Body) =
    if isNull body.Shape then (body.Shape, true)
    elif body.Shape.ShapeCase = Shape.ShapeOneofCase.CachedRef then
        match resolver with
        | Some r ->
            match MeshResolver.resolve body.Shape.CachedRef.MeshId r with
            | Some resolved -> (resolved, false)
            | None -> (body.Shape, true)
        | None -> (body.Shape, true)
    elif body.Shape.ShapeCase = Shape.ShapeOneofCase.ShapeRef then
        // Resolve ShapeRef via RegisteredShapes map
        if not (isNull simState) && not (isNull simState.RegisteredShapes) then
            let handle = body.Shape.ShapeRef.ShapeHandle
            let found = simState.RegisteredShapes |> Seq.tryFind (fun rs -> rs.ShapeHandle = handle)
            match found with
            | Some rs when not (isNull rs.Shape) -> (rs.Shape, false)
            | _ -> (body.Shape, true)
        else (body.Shape, true)
    else (body.Shape, false)

/// Create a custom mesh entity from CustomMeshData.
let private createCustomEntity (game: Game) (scene: Scene) (meshData: ShapeGeometry.CustomMeshData) (color: StrideColor) (wireframe: bool) (pos: Vector3) (rot: Quaternion) =
    let material =
        if wireframe then game.CreateFlatMaterial(System.Nullable<StrideColor>(color))
        else game.CreateMaterial(color)
    let gd = game.GraphicsDevice
    let model = createModelFromMeshData gd meshData material
    let entity = Entity()
    let mc = entity.GetOrCreate<ModelComponent>()
    mc.Model <- model
    entity.Transform.Position <- pos
    entity.Transform.Rotation <- rot
    entity.Scene <- scene
    entity

/// Create entity for a compound shape: parent with child entities.
let rec private createCompoundEntity (game: Game) (scene: Scene) (body: Body) (wireframe: bool) (renderShape: Shape) =
    let compound = renderShape.Compound
    if isNull compound || compound.Children.Count = 0 then
        // Fallback: placeholder sphere
        createPrimitiveEntity game scene body wireframe renderShape
    else
        let parentEntity = Entity(body.Id)
        parentEntity.Transform.Position <- protoVec3ToStride body.Position
        parentEntity.Transform.Rotation <- protoQuatToStride body.Orientation

        for child in compound.Children do
            if not (isNull child.Shape) then
                let childColor =
                    match protoColorToStride body.Color with
                    | Some c -> c
                    | None -> ShapeGeometry.defaultColor child.Shape
                let childEntity =
                    if ShapeGeometry.isCustomShape child.Shape then
                        match ShapeGeometry.buildCustomMesh child.Shape childColor with
                        | Some meshData ->
                            createCustomEntity game scene meshData childColor wireframe Vector3.Zero Quaternion.Identity
                        | None ->
                            createPrimitiveEntityDirect game child.Shape childColor wireframe Vector3.Zero Quaternion.Identity scene
                    elif not (isNull child.Shape) && child.Shape.ShapeCase = Shape.ShapeOneofCase.Compound then
                        // Nested compound: create sub-compound (simplified - render as children directly)
                        let subEntity = Entity()
                        for subChild in child.Shape.Compound.Children do
                            if not (isNull subChild.Shape) then
                                let sc = ShapeGeometry.defaultColor subChild.Shape
                                let se =
                                    if ShapeGeometry.isCustomShape subChild.Shape then
                                        match ShapeGeometry.buildCustomMesh subChild.Shape sc with
                                        | Some md -> createCustomEntity game scene md sc wireframe Vector3.Zero Quaternion.Identity
                                        | None -> createPrimitiveEntityDirect game subChild.Shape sc wireframe Vector3.Zero Quaternion.Identity scene
                                    else
                                        createPrimitiveEntityDirect game subChild.Shape sc wireframe Vector3.Zero Quaternion.Identity scene
                                se.Transform.Position <- protoVec3ToStride subChild.LocalPosition
                                se.Transform.Rotation <- protoQuatToStride subChild.LocalOrientation
                                subEntity.AddChild(se)
                        subEntity
                    else
                        createPrimitiveEntityDirect game child.Shape childColor wireframe Vector3.Zero Quaternion.Identity scene

                childEntity.Transform.Position <- protoVec3ToStride child.LocalPosition
                childEntity.Transform.Rotation <- protoQuatToStride child.LocalOrientation
                parentEntity.AddChild(childEntity)

        parentEntity.Scene <- scene
        parentEntity

/// Create a primitive entity (using Create3DPrimitive).
and private createPrimitiveEntity (game: Game) (scene: Scene) (body: Body) (wireframe: bool) (renderShape: Shape) =
    let color = bodyColor body renderShape
    let primType = ShapeGeometry.primitiveType renderShape
    let material =
        if wireframe then game.CreateFlatMaterial(System.Nullable<StrideColor>(color))
        else game.CreateMaterial(color)
    let size = ShapeGeometry.shapeSize renderShape
    let options = Primitive3DEntityOptions(Material = material, Size = size)
    let entity = game.Create3DPrimitive(primType, options)
    entity.Transform.Position <- protoVec3ToStride body.Position
    entity.Transform.Rotation <- protoQuatToStride body.Orientation
    entity.Scene <- scene
    entity

/// Create a standalone primitive entity from shape/color without a Body.
and private createPrimitiveEntityDirect (game: Game) (shape: Shape) (color: StrideColor) (wireframe: bool) (pos: Vector3) (rot: Quaternion) (scene: Scene) =
    let primType = ShapeGeometry.primitiveType shape
    let material =
        if wireframe then game.CreateFlatMaterial(System.Nullable<StrideColor>(color))
        else game.CreateMaterial(color)
    let size = ShapeGeometry.shapeSize shape
    let options = Primitive3DEntityOptions(Material = material, Size = size)
    let entity = game.Create3DPrimitive(primType, options)
    entity.Transform.Position <- pos
    entity.Transform.Rotation <- rot
    // Don't set scene here - caller manages parenting
    entity

/// Create entity dispatching between custom mesh, compound, and primitive paths.
let private createEntity (game: Game) (scene: Scene) (body: Body) (wireframe: bool) (renderShape: Shape) =
    if isNull renderShape then
        createPrimitiveEntity game scene body wireframe renderShape
    elif renderShape.ShapeCase = Shape.ShapeOneofCase.Compound then
        createCompoundEntity game scene body wireframe renderShape
    elif ShapeGeometry.isCustomShape renderShape then
        let color = bodyColor body renderShape
        match ShapeGeometry.buildCustomMesh renderShape color with
        | Some meshData ->
            let pos = protoVec3ToStride body.Position
            let rot = protoQuatToStride body.Orientation
            createCustomEntity game scene meshData color wireframe pos rot
        | None ->
            // Degenerate shape: fallback to primitive placeholder
            createPrimitiveEntity game scene body wireframe renderShape
    else
        createPrimitiveEntity game scene body wireframe renderShape

let private updateEntity (entity: Entity) (body: Body) =
    entity.Transform.Position <- protoVec3ToStride body.Position
    entity.Transform.Rotation <- protoQuatToStride body.Orientation

/// Applies a simulation state snapshot to the 3D scene.
let applyState (game: Game) (scene: Scene) (state: SceneState) (simState: SimulationState) (resolver: MeshResolver.MeshResolverState option) =
    if isNull simState || isNull simState.Bodies then state
    else

    let incomingIds = simState.Bodies |> Seq.map (fun b -> b.Id) |> Set.ofSeq

    // Remove entities no longer in state
    let bodiesToRemove =
        state.Bodies
        |> Map.filter (fun id _ -> not (Set.contains id incomingIds))

    for kvp in bodiesToRemove do
        kvp.Value.Scene <- null

    let mutable updatedBodies =
        state.Bodies
        |> Map.filter (fun id _ -> Set.contains id incomingIds)

    let mutable updatedPlaceholders = state.Placeholders |> Set.filter (fun id -> Set.contains id incomingIds)

    for body in simState.Bodies do
        let renderShape, isPlaceholder = resolveShape resolver simState body
        match Map.tryFind body.Id updatedBodies with
        | Some entity ->
            if Set.contains body.Id updatedPlaceholders && not isPlaceholder then
                entity.Scene <- null
                let newEntity = createEntity game scene body state.Wireframe renderShape
                updatedBodies <- Map.add body.Id newEntity updatedBodies
                updatedPlaceholders <- Set.remove body.Id updatedPlaceholders
            else
                updateEntity entity body
        | None ->
            let entity = createEntity game scene body state.Wireframe renderShape
            updatedBodies <- Map.add body.Id entity updatedBodies
            if isPlaceholder then
                updatedPlaceholders <- Set.add body.Id updatedPlaceholders

    { state with
        Bodies = updatedBodies
        Placeholders = updatedPlaceholders
        SimTime = simState.Time
        SimRunning = simState.Running }

/// Toggles wireframe rendering mode.
let applyWireframe (game: Game) (cmd: ToggleWireframe) (state: SceneState) =
    if cmd.Enabled = state.Wireframe then state
    else
        for kvp in state.Bodies do
            kvp.Value.Scene <- null
        { state with Bodies = Map.empty; Wireframe = cmd.Enabled }

/// Gets whether wireframe rendering mode is currently enabled.
let isWireframe (state: SceneState) = state.Wireframe

/// Gets the simulation time from the last applied state snapshot.
let simulationTime (state: SceneState) = state.SimTime

/// Gets whether the simulation was running in the last applied state snapshot.
let isRunning (state: SceneState) = state.SimRunning

/// Apply demo metadata from a SetDemoMetadata view command.
let applyDemoMetadata (cmd: SetDemoMetadata) (state: SceneState) =
    { state with
        DemoName = if System.String.IsNullOrEmpty(cmd.Name) then None else Some cmd.Name
        DemoDescription = if System.String.IsNullOrEmpty(cmd.Description) then None else Some cmd.Description }

/// Gets the current demo name.
let demoName (state: SceneState) = state.DemoName

/// Gets the current demo description.
let demoDescription (state: SceneState) = state.DemoDescription

/// Apply a narration text update. Empty string clears narration.
let applyNarration (text: string) (state: SceneState) =
    { state with NarrationText = if System.String.IsNullOrEmpty(text) then None else Some text }

/// Gets the current narration text.
let narrationText (state: SceneState) = state.NarrationText
