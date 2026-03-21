module PhysicsViewer.SceneManager

open Stride.Core.Mathematics
open Stride.Engine
open Stride.Games
open Stride.CommunityToolkit.Engine
open Stride.CommunityToolkit.Bepu
open Stride.CommunityToolkit.Rendering.ProceduralModels
open PhysicsSandbox.Shared.Contracts

/// <summary>
/// Discriminated union classifying proto Shape messages into known geometry kinds for color and primitive mapping.
/// </summary>
type ShapeKind =
    /// <summary>A spherical body.</summary>
    | Sphere
    /// <summary>A box-shaped body.</summary>
    | Box
    /// <summary>An unrecognized or null shape.</summary>
    | Unknown

/// <summary>
/// Tracks the 3D scene state, mapping simulation body IDs to Stride entities and storing simulation metadata.
/// </summary>
type SceneState =
    { /// <summary>Map from body ID to the corresponding Stride Entity in the scene.</summary>
      Bodies: Map<string, Entity>
      /// <summary>The simulation time from the last applied state snapshot.</summary>
      SimTime: float
      /// <summary>Whether the simulation was running in the last applied state snapshot.</summary>
      SimRunning: bool
      /// <summary>Whether wireframe rendering mode is currently enabled.</summary>
      Wireframe: bool }

/// <summary>
/// Creates an empty scene state with no bodies, zero simulation time, and wireframe disabled.
/// </summary>
/// <returns>A new SceneState ready to receive simulation snapshots.</returns>
let create () =
    { Bodies = Map.empty
      SimTime = 0.0
      SimRunning = false
      Wireframe = false }

/// <summary>
/// Classifies a proto Shape message into a ShapeKind discriminated union value.
/// </summary>
/// <param name="shape">The proto Shape to classify; null shapes return Unknown.</param>
/// <returns>The corresponding ShapeKind.</returns>
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

/// <summary>
/// Applies a simulation state snapshot to the 3D scene, adding new entities for new bodies,
/// updating positions and orientations for existing bodies, and removing entities for bodies
/// no longer present in the snapshot.
/// </summary>
/// <param name="game">The Stride Game instance used to create materials and primitives.</param>
/// <param name="scene">The Stride Scene to add or remove entities from.</param>
/// <param name="state">The current scene state to update.</param>
/// <param name="simState">The simulation state snapshot from the server.</param>
/// <returns>An updated SceneState reflecting the new simulation snapshot.</returns>
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

/// <summary>
/// Toggles wireframe rendering mode. When the mode changes, all existing entities are removed
/// so that applyState can recreate them with the appropriate material (flat for wireframe, lit otherwise).
/// </summary>
/// <param name="game">The Stride Game instance (unused directly but part of the public API for consistency).</param>
/// <param name="cmd">The ToggleWireframe command specifying the desired wireframe state.</param>
/// <param name="state">The current scene state.</param>
/// <returns>An updated SceneState with the new wireframe mode; bodies are cleared if the mode changed.</returns>
let applyWireframe (game: Game) (cmd: ToggleWireframe) (state: SceneState) =
    if cmd.Enabled = state.Wireframe then state
    else
        // Remove all existing entities — applyState will recreate them with new materials
        for kvp in state.Bodies do
            kvp.Value.Scene <- null
        { state with Bodies = Map.empty; Wireframe = cmd.Enabled }

/// <summary>Gets whether wireframe rendering mode is currently enabled.</summary>
let isWireframe (state: SceneState) = state.Wireframe

/// <summary>Gets the simulation time from the last applied state snapshot.</summary>
let simulationTime (state: SceneState) = state.SimTime

/// <summary>Gets whether the simulation was running in the last applied state snapshot.</summary>
let isRunning (state: SceneState) = state.SimRunning
