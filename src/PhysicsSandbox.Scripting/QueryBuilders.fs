/// <summary>Convenience wrappers for physics query operations (raycast, sweep cast, overlap).</summary>
module PhysicsSandbox.Scripting.QueryBuilders

open PhysicsClient.Session
open PhysicsSandbox.Shared.Contracts
open PhysicsSandbox.Scripting.Vec3Builders

let private hitToTuple (h: RayHit) =
    (h.BodyId, toTuple h.Position, toTuple h.Normal, h.Distance)

let raycast (session: Session) (origin: float * float * float) (direction: float * float * float) (maxDistance: float) =
    match PhysicsClient.SimulationCommands.raycast session origin direction maxDistance false None with
    | Ok response when response.Hit ->
        response.Hits |> Seq.map hitToTuple |> Seq.toList
    | _ -> []

let raycastAll (session: Session) (origin: float * float * float) (direction: float * float * float) (maxDistance: float) =
    match PhysicsClient.SimulationCommands.raycast session origin direction maxDistance true None with
    | Ok response when response.Hit ->
        response.Hits |> Seq.map hitToTuple |> Seq.toList
    | _ -> []

let sweepSphere (session: Session) (radius: float) (startPosition: float * float * float) (direction: float * float * float) (maxDistance: float) =
    let sphere = Sphere()
    sphere.Radius <- radius
    let shape = Shape()
    shape.Sphere <- sphere
    match PhysicsClient.SimulationCommands.sweepCast session shape startPosition direction maxDistance None None with
    | Ok response when response.Hit -> Some (hitToTuple response.Closest)
    | _ -> None

let overlapSphere (session: Session) (radius: float) (position: float * float * float) =
    let sphere = Sphere()
    sphere.Radius <- radius
    let shape = Shape()
    shape.Sphere <- sphere
    match PhysicsClient.SimulationCommands.overlap session shape position None None with
    | Ok response -> response.BodyIds |> Seq.toList
    | _ -> []
