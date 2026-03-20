module PhysicsViewer.SceneManager

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Games
open Stride.CommunityToolkit.Engine
open Stride.CommunityToolkit.Bepu
open Stride.CommunityToolkit.Rendering.ProceduralModels
open PhysicsSandbox.Shared.Contracts

type ShapeKind =
    | Sphere
    | Box
    | Unknown

type SceneState =
    { Bodies: Map<string, Entity>
      SimTime: float
      SimRunning: bool
      Wireframe: bool }

let create () =
    { Bodies = Map.empty
      SimTime = 0.0
      SimRunning = false
      Wireframe = false }

let classifyShape (shape: Shape) =
    if isNull shape then Unknown
    elif shape.ShapeCase = Shape.ShapeOneofCase.Sphere then Sphere
    elif shape.ShapeCase = Shape.ShapeOneofCase.Box then Box
    else Unknown

let private shapeColor kind =
    match kind with
    | Sphere -> Color.Blue
    | Box -> Color.Orange
    | Unknown -> Color.Red

let private shapePrimitive kind =
    match kind with
    | Sphere -> PrimitiveModelType.Sphere
    | Box -> PrimitiveModelType.Cube
    | Unknown -> PrimitiveModelType.Sphere

let private protoVec3ToStride (v: Vec3) =
    if isNull v then Vector3.Zero
    else Vector3(float32 v.X, float32 v.Y, float32 v.Z)

let private protoQuatToStride (v: Vec4) =
    if isNull v then Quaternion.Identity
    else Quaternion(float32 v.X, float32 v.Y, float32 v.Z, float32 v.W)

let private createEntity (game: Game) (scene: Scene) (body: Body) (wireframe: bool) =
    let kind = classifyShape body.Shape
    let color = shapeColor kind
    let primType = shapePrimitive kind
    let material =
        if wireframe then
            game.CreateFlatMaterial(System.Nullable<Color>(color))
        else
            game.CreateMaterial(color)
    let options = Bepu3DPhysicsOptions(Material = material, IncludeCollider = false)
    let entity = game.Create3DPrimitive(primType, options)
    entity.Transform.Position <- protoVec3ToStride body.Position
    entity.Transform.Rotation <- protoQuatToStride body.Orientation
    entity.Scene <- scene
    entity

let private updateEntity (entity: Entity) (body: Body) =
    entity.Transform.Position <- protoVec3ToStride body.Position
    entity.Transform.Rotation <- protoQuatToStride body.Orientation

let applyState (game: Game) (scene: Scene) (state: SceneState) (simState: SimulationState) =
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

    // Add or update
    for body in simState.Bodies do
        match Map.tryFind body.Id updatedBodies with
        | Some entity ->
            updateEntity entity body
        | None ->
            let entity = createEntity game scene body state.Wireframe
            updatedBodies <- Map.add body.Id entity updatedBodies

    { state with
        Bodies = updatedBodies
        SimTime = simState.Time
        SimRunning = simState.Running }

let applyWireframe (game: Game) (cmd: ToggleWireframe) (state: SceneState) =
    if cmd.Enabled = state.Wireframe then state
    else
        // Remove all existing entities — applyState will recreate them with new materials
        for kvp in state.Bodies do
            kvp.Value.Scene <- null
        { state with Bodies = Map.empty; Wireframe = cmd.Enabled }

let isWireframe (state: SceneState) = state.Wireframe

let simulationTime (state: SceneState) = state.SimTime

let isRunning (state: SceneState) = state.SimRunning
