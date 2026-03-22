module PhysicsSimulation.QueryHandler

open System.Numerics
open PhysicsSandbox.Shared.Contracts
open PhysicsSimulation.SimulationWorld
open BepuFSharp

let private toVector3 (v: Vec3) =
    if isNull v then Vector3.Zero
    else Vector3(float32 v.X, float32 v.Y, float32 v.Z)

let private toQuaternion (v: Vec4) =
    if isNull v then Quaternion.Identity
    else Quaternion(float32 v.X, float32 v.Y, float32 v.Z, float32 v.W)

let private fromVector3 (v: Vector3) =
    let r = Vec3()
    r.X <- double v.X
    r.Y <- double v.Y
    r.Z <- double v.Z
    r

let private toCollisionFilter (mask: uint32) =
    if mask = 0u then None
    else Some { Group = 0u; Mask = mask }

/// Resolve a BepuFSharp BodyId or StaticId to the user-facing string ID.
let private resolveHitId (world: World) (body: BodyId voption) (staticBody: StaticId voption) =
    let bodiesMap = bodies world
    let mutable found = ""
    for kvp in bodiesMap do
        if System.String.IsNullOrEmpty found then
            match body with
            | ValueSome bodyId when kvp.Value.BepuBodyId = bodyId && not kvp.Value.IsStatic ->
                found <- kvp.Key
            | _ -> ()
            match staticBody with
            | ValueSome staticId when kvp.Value.IsStatic && kvp.Value.BepuStaticId = staticId ->
                found <- kvp.Key
            | _ -> ()
    found

let private toProtoRayHit (world: World) (hit: BepuFSharp.RayHit) =
    let rh = RayHit()
    rh.BodyId <- resolveHitId world hit.Body hit.Static
    rh.Position <- fromVector3 hit.Position
    rh.Normal <- fromVector3 hit.Normal
    rh.Distance <- double hit.Distance
    rh

let handleRaycast (world: World) (req: RaycastRequest) =
    let origin = toVector3 req.Origin
    let direction = toVector3 req.Direction
    let maxDist = float32 req.MaxDistance
    let filter = toCollisionFilter req.CollisionMask
    let pw = physicsWorld world
    let response = RaycastResponse()

    if req.AllHits then
        let hits = PhysicsWorld.raycastAll origin direction maxDist filter pw
        if hits.Length > 0 then
            response.Hit <- true
            for hit in hits do
                response.Hits.Add(toProtoRayHit world hit)
    else
        match PhysicsWorld.raycast origin direction maxDist filter pw with
        | Some hit ->
            response.Hit <- true
            response.Hits.Add(toProtoRayHit world hit)
        | None ->
            response.Hit <- false

    response

let handleSweepCast (world: World) (req: SweepCastRequest) =
    let response = SweepCastResponse()
    let pw = physicsWorld world

    match convertShape world req.Shape with
    | Error _ ->
        response.Hit <- false
        response
    | Ok (physShape, _) ->
        let pose = Pose.create (toVector3 req.StartPosition) (toQuaternion req.Orientation)
        let direction = toVector3 req.Direction
        let maxDist = float32 req.MaxDistance
        let filter = toCollisionFilter req.CollisionMask

        match PhysicsWorld.sweepCast physShape pose direction maxDist filter pw with
        | Some hit ->
            response.Hit <- true
            let rh = RayHit()
            rh.BodyId <- resolveHitId world hit.Body hit.Static
            rh.Position <- fromVector3 hit.Position
            rh.Normal <- fromVector3 hit.Normal
            rh.Distance <- double hit.Distance
            response.Closest <- rh
        | None ->
            response.Hit <- false

        response

let handleOverlap (world: World) (req: OverlapRequest) =
    let response = OverlapResponse()
    let pw = physicsWorld world

    match convertShape world req.Shape with
    | Error _ -> response
    | Ok (physShape, _) ->
        let pose = Pose.create (toVector3 req.Position) (toQuaternion req.Orientation)
        let filter = toCollisionFilter req.CollisionMask

        let results = PhysicsWorld.overlap physShape pose filter pw
        for result in results do
            let id = resolveHitId world result.Body result.Static
            if not (System.String.IsNullOrEmpty id) then
                response.BodyIds.Add(id)

        response
