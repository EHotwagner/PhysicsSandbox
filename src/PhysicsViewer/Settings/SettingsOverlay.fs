module PhysicsViewer.Settings.SettingsOverlay

open Stride.Core.Mathematics
open Stride.Input
open Stride.Profiling
open PhysicsViewer.Settings.ViewerSettings

type SettingsCategory =
    | Display
    | Quality

type MenuItem =
    { Label: string
      Options: string list
      CurrentIndex: int }

type OverlayState =
    { Visible: bool
      Category: SettingsCategory
      SelectedRow: int
      Settings: ViewerSettings
      DisplayItems: MenuItem list
      QualityItems: MenuItem list }

let private resolutionOptions = [ "1280x720"; "1920x1080"; "2560x1440" ]

let private aaOptions = [ "Off"; "X2"; "X4"; "X8" ]
let private shadowOptions = [ "Off"; "Low"; "Medium"; "High" ]
let private filterOptions = [ "Point"; "Linear"; "Anisotropic" ]
let private vsyncOptions = [ "Off"; "On" ]

let private resIndex (s: ViewerSettings) =
    let key = $"{s.ResolutionWidth}x{s.ResolutionHeight}"
    match resolutionOptions |> List.tryFindIndex ((=) key) with
    | Some i -> i
    | None -> 0

let private aaIndex (aa: AntiAliasingLevel) = match aa with AntiAliasingLevel.Off -> 0 | X2 -> 1 | X4 -> 2 | X8 -> 3
let private sqIndex (sq: ShadowQuality) = match sq with ShadowQuality.Off -> 0 | Low -> 1 | Medium -> 2 | High -> 3
let private tfIndex = function Point -> 0 | Linear -> 1 | Anisotropic -> 2
let private vsIndex v = if v then 1 else 0

let private buildDisplayItems (s: ViewerSettings) =
    [ { Label = "Resolution"; Options = resolutionOptions; CurrentIndex = resIndex s }
      { Label = "Fullscreen"; Options = [ "Off"; "On" ]; CurrentIndex = if s.IsFullscreen then 1 else 0 } ]

let private buildQualityItems (s: ViewerSettings) =
    [ { Label = "Anti-Aliasing"; Options = aaOptions; CurrentIndex = aaIndex s.AntiAliasing }
      { Label = "Shadows"; Options = shadowOptions; CurrentIndex = sqIndex s.ShadowQuality }
      { Label = "Texture Filter"; Options = filterOptions; CurrentIndex = tfIndex s.TextureFiltering }
      { Label = "VSync"; Options = vsyncOptions; CurrentIndex = vsIndex s.VSync } ]

let create (settings: ViewerSettings) =
    { Visible = false
      Category = Display
      SelectedRow = 0
      Settings = settings
      DisplayItems = buildDisplayItems settings
      QualityItems = buildQualityItems settings }

let isVisible state = state.Visible

let toggle state =
    { state with Visible = not state.Visible; SelectedRow = 0 }

let private currentItems state =
    match state.Category with
    | Display -> state.DisplayItems
    | Quality -> state.QualityItems

let private itemCount state = (currentItems state).Length

let private updateItem (items: MenuItem list) row f =
    items |> List.mapi (fun i item -> if i = row then f item else item)

let private cycleRight (item: MenuItem) =
    { item with CurrentIndex = (item.CurrentIndex + 1) % item.Options.Length }

let private cycleLeft (item: MenuItem) =
    { item with CurrentIndex = (item.CurrentIndex - 1 + item.Options.Length) % item.Options.Length }

let private settingsFromState (state: OverlayState) =
    let di = state.DisplayItems
    let qi = state.QualityItems
    let resParts = di.[0].Options.[di.[0].CurrentIndex].Split('x')
    let w = int resParts.[0]
    let h = int resParts.[1]
    let fs = di.[1].CurrentIndex = 1
    let aa : AntiAliasingLevel = match qi.[0].CurrentIndex with 1 -> X2 | 2 -> X4 | 3 -> X8 | _ -> AntiAliasingLevel.Off
    let sq : ShadowQuality = match qi.[1].CurrentIndex with 1 -> Low | 2 -> Medium | 3 -> High | _ -> ShadowQuality.Off
    let tf = match qi.[2].CurrentIndex with 0 -> Point | 2 -> Anisotropic | _ -> Linear
    let vs = qi.[3].CurrentIndex = 1
    { ResolutionWidth = w; ResolutionHeight = h; IsFullscreen = fs
      AntiAliasing = aa; ShadowQuality = sq; TextureFiltering = tf; VSync = vs }

let handleInput (input: InputManager) (state: OverlayState) =
    if not state.Visible then state, None
    else
        let mutable s = state
        let mutable changed = false

        // Tab to switch categories
        if input.IsKeyPressed(Keys.Tab) then
            s <- { s with
                    Category = (match s.Category with Display -> Quality | Quality -> Display)
                    SelectedRow = 0 }

        // Navigate up/down
        if input.IsKeyPressed(Keys.Up) then
            s <- { s with SelectedRow = max 0 (s.SelectedRow - 1) }
        if input.IsKeyPressed(Keys.Down) then
            s <- { s with SelectedRow = min (itemCount s - 1) (s.SelectedRow + 1) }

        // Left/Right to change value
        if input.IsKeyPressed(Keys.Right) || input.IsKeyPressed(Keys.Return) then
            match s.Category with
            | Display ->
                s <- { s with DisplayItems = updateItem s.DisplayItems s.SelectedRow cycleRight }
            | Quality ->
                s <- { s with QualityItems = updateItem s.QualityItems s.SelectedRow cycleRight }
            changed <- true
        if input.IsKeyPressed(Keys.Left) then
            match s.Category with
            | Display ->
                s <- { s with DisplayItems = updateItem s.DisplayItems s.SelectedRow cycleLeft }
            | Quality ->
                s <- { s with QualityItems = updateItem s.QualityItems s.SelectedRow cycleLeft }
            changed <- true

        if changed then
            let newSettings = settingsFromState s
            s <- { s with Settings = newSettings }
            s, Some newSettings
        else
            s, None

let render (debugText: DebugTextSystem) (state: OverlayState) =
    if not state.Visible then ()
    else
        let x = 20
        let mutable y = 60
        let lineH = 20

        debugText.Print("=== SETTINGS (Tab: switch, Arrows: navigate, F2: close) ===", Int2(x, y))
        y <- y + lineH

        // Category tabs
        let dispLabel = if state.Category = Display then "[Display]" else " Display "
        let qualLabel = if state.Category = Quality then "[Quality]" else " Quality "
        debugText.Print($"{dispLabel}  {qualLabel}", Int2(x, y))
        y <- y + lineH + 5

        let items = currentItems state
        for i in 0 .. items.Length - 1 do
            let item = items.[i]
            let marker = if i = state.SelectedRow then ">" else " "
            let value = item.Options.[item.CurrentIndex]
            debugText.Print($"{marker} {item.Label}: < {value} >", Int2(x, y))
            y <- y + lineH
