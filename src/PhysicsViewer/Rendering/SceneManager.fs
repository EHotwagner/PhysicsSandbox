module PhysicsViewer.SceneManager

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Games
open Stride.CommunityToolkit.Engine
open Stride.CommunityToolkit.Games
open PhysicsSandbox.Shared.Contracts
module MeshResolver = PhysicsViewer.Streaming.MeshResolver

type StrideColor = Stride.Core.Mathematics.Color
type ProtoColor = PhysicsSandbox.Shared.Contracts.Color

/// Tracks the 3D scene state, mapping simulation body IDs to Stride entities.
type SceneState =
    { Bodies: Map<string, Entity>
      Placeholders: Set<string> // Body IDs currently rendered as bounding box placeholders
      SimTime: float
      SimRunning: bool
      Wireframe: bool }

/// Creates an empty scene state.
let create () =
    { Bodies = Map.empty
      Placeholders = Set.empty
      SimTime = 0.0
      SimRunning = false
      Wireframe = false }

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

/// Determine the visual color for a body: use body color if set, otherwise default by shape type.
let private bodyColor (body: Body) : StrideColor =
    match protoColorToStride body.Color with
    | Some c -> c
    | None -> ShapeGeometry.defaultColor body.Shape

/// Returns (shape to render, isPlaceholder)
let private resolveShape (resolver: MeshResolver.MeshResolverState option) (body: Body) =
    if isNull body.Shape then (body.Shape, true)
    elif body.Shape.ShapeCase = Shape.ShapeOneofCase.CachedRef then
        match resolver with
        | Some r ->
            match MeshResolver.resolve body.Shape.CachedRef.MeshId r with
            | Some resolved -> (resolved, false)
            | None -> (body.Shape, true) // unresolved: use CachedRef bbox as placeholder
        | None -> (body.Shape, true)
    else (body.Shape, false)

let private createEntity (game: Game) (scene: Scene) (body: Body) (wireframe: bool) (renderShape: Shape) =
    let color =
        match protoColorToStride body.Color with
        | Some c -> c
        | None -> ShapeGeometry.defaultColor renderShape
    let primType = ShapeGeometry.primitiveType renderShape
    let material =
        if wireframe then
            game.CreateFlatMaterial(System.Nullable<StrideColor>(color))
        else
            game.CreateMaterial(color)
    let size = ShapeGeometry.shapeSize renderShape
    let options = Primitive3DEntityOptions(Material = material, Size = size)
    let entity = game.Create3DPrimitive(primType, options)
    entity.Transform.Position <- protoVec3ToStride body.Position
    entity.Transform.Rotation <- protoQuatToStride body.Orientation
    entity.Scene <- scene
    entity

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

    // Add or update
    for body in simState.Bodies do
        let renderShape, isPlaceholder = resolveShape resolver body
        match Map.tryFind body.Id updatedBodies with
        | Some entity ->
            // If this was a placeholder and mesh is now resolved, recreate the entity
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
        // Remove all existing entities — applyState will recreate them with new materials
        for kvp in state.Bodies do
            kvp.Value.Scene <- null
        { state with Bodies = Map.empty; Wireframe = cmd.Enabled }

/// Gets whether wireframe rendering mode is currently enabled.
let isWireframe (state: SceneState) = state.Wireframe

/// Gets the simulation time from the last applied state snapshot.
let simulationTime (state: SceneState) = state.SimTime

/// Gets whether the simulation was running in the last applied state snapshot.
let isRunning (state: SceneState) = state.SimRunning
