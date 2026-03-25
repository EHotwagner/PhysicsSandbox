/// <summary>Commands for controlling the 3D viewer: camera positioning, zoom, and rendering modes.</summary>
module PhysicsClient.ViewCommands

open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Session
open PhysicsClient.Vec3Helpers

/// <summary>Sets the viewer camera position and look-at target. The up vector is always (0, 1, 0).</summary>
/// <param name="session">The active server session.</param>
/// <param name="position">Camera world-space position as (x, y, z).</param>
/// <param name="target">Point the camera looks at as (x, y, z).</param>
let setCamera (session: Session) (position: float * float * float) (target: float * float * float) : Result<unit, string> =
    let sc = SetCamera()
    sc.Position <- toVec3 position
    sc.Target <- toVec3 target
    sc.Up <- toVec3 (0.0, 1.0, 0.0)
    let cmd = ViewCommand()
    cmd.SetCamera <- sc
    sendViewCommand session cmd

/// <summary>Sets the viewer camera zoom level.</summary>
/// <param name="session">The active server session.</param>
/// <param name="level">Zoom level where 1.0 is the default view distance.</param>
let setZoom (session: Session) (level: float) : Result<unit, string> =
    let sz = SetZoom()
    sz.Level <- level
    let cmd = ViewCommand()
    cmd.SetZoom <- sz
    sendViewCommand session cmd

/// <summary>Toggles wireframe rendering mode in the 3D viewer.</summary>
/// <param name="session">The active server session.</param>
/// <param name="enabled">True to enable wireframe rendering, false for solid rendering.</param>
let wireframe (session: Session) (enabled: bool) : Result<unit, string> =
    let tw = ToggleWireframe()
    tw.Enabled <- enabled
    let cmd = ViewCommand()
    cmd.ToggleWireframe <- tw
    sendViewCommand session cmd

/// <summary>Sets demo metadata (name and description) displayed as an overlay in the 3D viewer.</summary>
/// <param name="session">The active server session.</param>
/// <param name="name">Short demo name (e.g., "Hello Drop").</param>
/// <param name="description">One-line demo description.</param>
let setDemoMetadata (session: Session) (name: string) (description: string) : Result<unit, string> =
    let dm = SetDemoMetadata()
    dm.Name <- name
    dm.Description <- description
    let cmd = ViewCommand()
    cmd.SetDemoMetadata <- dm
    sendViewCommand session cmd

let smoothCamera (session: Session) (position: float * float * float) (target: float * float * float) (durationSeconds: float) : Result<unit, string> =
    let sc = SmoothCamera()
    sc.Position <- toVec3 position
    sc.Target <- toVec3 target
    sc.Up <- toVec3 (0.0, 1.0, 0.0)
    sc.DurationSeconds <- durationSeconds
    let cmd = ViewCommand()
    cmd.SmoothCamera <- sc
    sendViewCommand session cmd

let smoothCameraWithZoom (session: Session) (position: float * float * float) (target: float * float * float) (durationSeconds: float) (zoomLevel: float) : Result<unit, string> =
    let sc = SmoothCamera()
    sc.Position <- toVec3 position
    sc.Target <- toVec3 target
    sc.Up <- toVec3 (0.0, 1.0, 0.0)
    sc.DurationSeconds <- durationSeconds
    sc.ZoomLevel <- zoomLevel
    let cmd = ViewCommand()
    cmd.SmoothCamera <- sc
    sendViewCommand session cmd

let cameraLookAt (session: Session) (bodyId: string) (durationSeconds: float) : Result<unit, string> =
    let la = CameraLookAt()
    la.BodyId <- bodyId
    la.DurationSeconds <- durationSeconds
    let cmd = ViewCommand()
    cmd.CameraLookAt <- la
    sendViewCommand session cmd

let cameraFollow (session: Session) (bodyId: string) : Result<unit, string> =
    let cf = CameraFollow()
    cf.BodyId <- bodyId
    let cmd = ViewCommand()
    cmd.CameraFollow <- cf
    sendViewCommand session cmd

let cameraOrbit (session: Session) (bodyId: string) (durationSeconds: float) (degrees: float) : Result<unit, string> =
    let co = CameraOrbit()
    co.BodyId <- bodyId
    co.DurationSeconds <- durationSeconds
    co.Degrees <- degrees
    let cmd = ViewCommand()
    cmd.CameraOrbit <- co
    sendViewCommand session cmd

let cameraChase (session: Session) (bodyId: string) (offset: float * float * float) : Result<unit, string> =
    let cc = CameraChase()
    cc.BodyId <- bodyId
    cc.Offset <- toVec3 offset
    let cmd = ViewCommand()
    cmd.CameraChase <- cc
    sendViewCommand session cmd

let cameraFrameBodies (session: Session) (bodyIds: string list) : Result<unit, string> =
    let fb = CameraFrameBodies()
    for id in bodyIds do fb.BodyIds.Add(id)
    let cmd = ViewCommand()
    cmd.CameraFrameBodies <- fb
    sendViewCommand session cmd

let cameraShake (session: Session) (intensity: float) (durationSeconds: float) : Result<unit, string> =
    let cs = CameraShake()
    cs.Intensity <- intensity
    cs.DurationSeconds <- durationSeconds
    let cmd = ViewCommand()
    cmd.CameraShake <- cs
    sendViewCommand session cmd

let cameraStop (session: Session) : Result<unit, string> =
    let cmd = ViewCommand()
    cmd.CameraStop <- CameraStop()
    sendViewCommand session cmd

let setNarration (session: Session) (text: string) : Result<unit, string> =
    let sn = SetNarration()
    sn.Text <- text
    let cmd = ViewCommand()
    cmd.SetNarration <- sn
    sendViewCommand session cmd
