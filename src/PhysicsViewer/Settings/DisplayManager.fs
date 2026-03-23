module PhysicsViewer.Settings.DisplayManager

open Stride.Engine
open Stride.Games
open Stride.Graphics
open Stride.Rendering.Lights
open PhysicsViewer.Settings.ViewerSettings

type DisplayState =
    { Game: Game
      Settings: ViewerSettings
      PreviousWindowedWidth: int
      PreviousWindowedHeight: int }

let private getGraphicsDeviceManager (game: Game) : GraphicsDeviceManager option =
    let gm = game.Services.GetService<GraphicsDeviceManager>()
    if isNull (box gm) then None else Some gm

let private aaToMsaa (aa: AntiAliasingLevel) =
    match aa with
    | AntiAliasingLevel.Off -> MultisampleCount.None
    | X2 -> MultisampleCount.X2
    | X4 -> MultisampleCount.X4
    | X8 -> MultisampleCount.X8

let private sqToCascadeCount (sq: ShadowQuality) =
    match sq with
    | ShadowQuality.Off -> LightShadowMapCascadeCount.OneCascade
    | Low -> LightShadowMapCascadeCount.OneCascade
    | Medium -> LightShadowMapCascadeCount.TwoCascades
    | High -> LightShadowMapCascadeCount.FourCascades

let private sqToEnabled (sq: ShadowQuality) =
    match sq with
    | ShadowQuality.Off -> false
    | _ -> true

let private applyGraphicsSettings (game: Game) (settings: ViewerSettings) =
    match getGraphicsDeviceManager game with
    | Some gm ->
        gm.PreferredBackBufferWidth <- settings.ResolutionWidth
        gm.PreferredBackBufferHeight <- settings.ResolutionHeight
        gm.SynchronizeWithVerticalRetrace <- settings.VSync
        gm.PreferredMultisampleCount <- aaToMsaa settings.AntiAliasing
        gm.ApplyChanges()
    | None -> ()

let private applyShadowSettings (game: Game) (settings: ViewerSettings) =
    // Find directional lights in the scene and configure shadows
    if not (isNull game.SceneSystem) && not (isNull game.SceneSystem.SceneInstance) then
        let scene = game.SceneSystem.SceneInstance.RootScene
        if not (isNull scene) then
            for entity in scene.Entities do
                let lightComp = entity.Get<LightComponent>()
                if not (isNull lightComp) then
                    match lightComp.Type with
                    | :? LightDirectional as directional ->
                        match directional.Shadow with
                        | :? LightDirectionalShadowMap as shadowMap ->
                            shadowMap.Enabled <- sqToEnabled settings.ShadowQuality
                            if sqToEnabled settings.ShadowQuality then
                                shadowMap.CascadeCount <- sqToCascadeCount settings.ShadowQuality
                        | _ ->
                            if sqToEnabled settings.ShadowQuality then
                                let shadowMap = LightDirectionalShadowMap()
                                shadowMap.Enabled <- true
                                shadowMap.CascadeCount <- sqToCascadeCount settings.ShadowQuality
                                directional.Shadow <- shadowMap
                    | _ -> ()

let create (game: Game) (settings: ViewerSettings) =
    if settings.IsFullscreen then
        game.Window.IsBorderLess <- true
        game.Window.IsFullscreen <- true
    applyGraphicsSettings game settings
    applyShadowSettings game settings
    { Game = game
      Settings = settings
      PreviousWindowedWidth = settings.ResolutionWidth
      PreviousWindowedHeight = settings.ResolutionHeight }

let applySettings (state: DisplayState) (settings: ViewerSettings) =
    let game = state.Game
    applyGraphicsSettings game settings
    applyShadowSettings game settings
    if settings.IsFullscreen <> state.Settings.IsFullscreen then
        game.Window.IsBorderLess <- settings.IsFullscreen
        game.Window.IsFullscreen <- settings.IsFullscreen
    { state with Settings = settings }

let toggleFullscreen (state: DisplayState) =
    let isFs = not state.Settings.IsFullscreen
    let game = state.Game
    game.Window.IsBorderLess <- isFs
    game.Window.IsFullscreen <- isFs
    let newSettings = { state.Settings with IsFullscreen = isFs }
    if isFs then
        { state with
            Settings = newSettings
            PreviousWindowedWidth = state.Settings.ResolutionWidth
            PreviousWindowedHeight = state.Settings.ResolutionHeight }
    else
        let restored =
            { newSettings with
                ResolutionWidth = state.PreviousWindowedWidth
                ResolutionHeight = state.PreviousWindowedHeight }
        applyGraphicsSettings game restored
        { state with Settings = restored }

let currentSettings (state: DisplayState) = state.Settings
