module PhysicsViewer.Settings.ViewerSettings

open System
open System.IO
open System.Text.Json

type AntiAliasingLevel = Off | X2 | X4 | X8
type ShadowQuality = Off | Low | Medium | High
type TextureFilteringMode = Point | Linear | Anisotropic

type ViewerSettings =
    { ResolutionWidth: int
      ResolutionHeight: int
      IsFullscreen: bool
      AntiAliasing: AntiAliasingLevel
      ShadowQuality: ShadowQuality
      TextureFiltering: TextureFilteringMode
      VSync: bool }

let private settingsPath =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "PhysicsSandbox", "viewer-settings.json")

let defaultSettings () =
    { ResolutionWidth = 1280
      ResolutionHeight = 720
      IsFullscreen = false
      AntiAliasing = AntiAliasingLevel.Off
      ShadowQuality = ShadowQuality.Medium
      TextureFiltering = TextureFilteringMode.Linear
      VSync = true }

let private aaToString (aa: AntiAliasingLevel) = match aa with AntiAliasingLevel.Off -> "Off" | X2 -> "X2" | X4 -> "X4" | X8 -> "X8"
let private aaFromString = function "X2" -> X2 | "X4" -> X4 | "X8" -> X8 | _ -> AntiAliasingLevel.Off
let private sqToString (sq: ShadowQuality) = match sq with ShadowQuality.Off -> "Off" | Low -> "Low" | Medium -> "Medium" | High -> "High"
let private sqFromString = function "Low" -> Low | "Medium" -> Medium | "High" -> High | _ -> ShadowQuality.Off
let private tfToString = function Point -> "Point" | Linear -> "Linear" | Anisotropic -> "Anisotropic"
let private tfFromString = function "Point" -> Point | "Anisotropic" -> Anisotropic | _ -> Linear

let save (settings: ViewerSettings) =
    let dir = Path.GetDirectoryName(settingsPath)
    Directory.CreateDirectory(dir) |> ignore
    use stream = new MemoryStream()
    use writer = new Utf8JsonWriter(stream, JsonWriterOptions(Indented = true))
    writer.WriteStartObject()
    writer.WriteNumber("ResolutionWidth", settings.ResolutionWidth)
    writer.WriteNumber("ResolutionHeight", settings.ResolutionHeight)
    writer.WriteBoolean("IsFullscreen", settings.IsFullscreen)
    writer.WriteString("AntiAliasing", aaToString settings.AntiAliasing)
    writer.WriteString("ShadowQuality", sqToString settings.ShadowQuality)
    writer.WriteString("TextureFiltering", tfToString settings.TextureFiltering)
    writer.WriteBoolean("VSync", settings.VSync)
    writer.WriteEndObject()
    writer.Flush()
    File.WriteAllBytes(settingsPath, stream.ToArray())

let load () =
    try
        if not (File.Exists settingsPath) then defaultSettings ()
        else
            let json = File.ReadAllText(settingsPath)
            use doc = JsonDocument.Parse(json)
            let root = doc.RootElement
            let getInt (name: string) def =
                let mutable el = System.Text.Json.JsonElement()
                if root.TryGetProperty(name, &el) then el.GetInt32() else def
            let getBool (name: string) def =
                let mutable el = System.Text.Json.JsonElement()
                if root.TryGetProperty(name, &el) then el.GetBoolean() else def
            let getString (name: string) def =
                let mutable el = System.Text.Json.JsonElement()
                if root.TryGetProperty(name, &el) then el.GetString() else def
            { ResolutionWidth = getInt "ResolutionWidth" 1280
              ResolutionHeight = getInt "ResolutionHeight" 720
              IsFullscreen = getBool "IsFullscreen" false
              AntiAliasing = aaFromString (getString "AntiAliasing" "Off")
              ShadowQuality = sqFromString (getString "ShadowQuality" "Medium")
              TextureFiltering = tfFromString (getString "TextureFiltering" "Linear")
              VSync = getBool "VSync" true }
    with _ -> defaultSettings ()
