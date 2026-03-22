module PhysicsViewer.SceneManager

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Games
open Stride.CommunityToolkit.Engine
open Stride.CommunityToolkit.Bepu
open PhysicsSandbox.Shared.Contracts

type StrideColor = Stride.Core.Mathematics.Color
type ProtoColor = PhysicsSandbox.Shared.Contracts.Color

/// Tracks the 3D scene state, mapping simulation body IDs to Stride entities.
type SceneState =
    { Bodies: Map<string, Entity>
      SimTime: float
      SimRunning: bool
      Wireframe: bool }

/// Creates an empty scene state.
let create () =
    { Bodies = Map.empty
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

let private createEntity (game: Game) (scene: Scene) (body: Body) (wireframe: bool) =
    let color = bodyColor body
    let primType = ShapeGeometry.primitiveType body.Shape
    let material =
        if wireframe then
            game.CreateFlatMaterial(System.Nullable<StrideColor>(color))
        else
            game.CreateMaterial(color)
    let size = ShapeGeometry.shapeSize body.Shape
    let options = Bepu3DPhysicsOptions(Material = material, IncludeCollider = false, Size = size)
    let entity = game.Create3DPrimitive(primType, options)
    entity.Transform.Position <- protoVec3ToStride body.Position
    entity.Transform.Rotation <- protoQuatToStride body.Orientation
    entity.Scene <- scene
    entity

let private updateEntity (entity: Entity) (body: Body) =
    entity.Transform.Position <- protoVec3ToStride body.Position
    entity.Transform.Rotation <- protoQuatToStride body.Orientation

/// Applies a simulation state snapshot to the 3D scene.
let applyState (game: Game) (scene: Scene) (state: SceneState) (simState: SimulationState) =
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
