/// <summary>Commands for controlling the 3D viewer: camera positioning, zoom, and rendering modes.</summary>
module PhysicsClient.ViewCommands

open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Session
open PhysicsClient.SimulationCommands

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
