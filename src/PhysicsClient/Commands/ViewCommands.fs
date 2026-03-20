module PhysicsClient.ViewCommands

open PhysicsSandbox.Shared.Contracts
open PhysicsClient.Session
open PhysicsClient.SimulationCommands

let setCamera (session: Session) (position: float * float * float) (target: float * float * float) : Result<unit, string> =
    let sc = SetCamera()
    sc.Position <- toVec3 position
    sc.Target <- toVec3 target
    sc.Up <- toVec3 (0.0, 1.0, 0.0)
    let cmd = ViewCommand()
    cmd.SetCamera <- sc
    sendViewCommand session cmd

let setZoom (session: Session) (level: float) : Result<unit, string> =
    let sz = SetZoom()
    sz.Level <- level
    let cmd = ViewCommand()
    cmd.SetZoom <- sz
    sendViewCommand session cmd

let wireframe (session: Session) (enabled: bool) : Result<unit, string> =
    let tw = ToggleWireframe()
    tw.Enabled <- enabled
    let cmd = ViewCommand()
    cmd.ToggleWireframe <- tw
    sendViewCommand session cmd
