/// <summary>MCP tool class for procedurally generating arrangements of bodies: random scatters, stacks, rows, grids, and pyramids.</summary>
module PhysicsSandbox.Mcp.GeneratorTools

open System
open System.ComponentModel
open System.Text
open ModelContextProtocol.Server
open PhysicsSandbox.Mcp.GrpcConnection
open PhysicsSandbox.Mcp.ClientAdapter
open PhysicsClient.IdGenerator

/// <summary>MCP server tool type for procedurally generating spatial arrangements of physics bodies.</summary>
[<McpServerToolType>]
type GeneratorTools() =

    /// <summary>Generates a random mix of spheres and boxes with randomized positions, sizes, and masses within a bounded volume.</summary>
    [<McpServerTool>]
    [<Description("Generate random bodies (mix of spheres and boxes)")>]
    static member generate_random_bodies(connection: GrpcConnection,
                                          [<Description("Number of bodies to create")>] count: int,
                                          [<Description("Random seed. Default: 0 (uses random seed). Provide a fixed positive integer for reproducible results.")>] seed: Nullable<int>) =
        if count <= 0 then "Error: count must be greater than 0"
        else
            let seedVal = if seed.HasValue then seed.Value else 0
            let rng = if seedVal = 0 then Random() else Random(seedVal)
            let sb = StringBuilder()
            sb.AppendLine($"Generated {count} random bodies:") |> ignore
            for _ in 1..count do
                let x = rng.NextDouble() * 10.0 - 5.0
                let y = rng.NextDouble() * 9.0 + 1.0
                let z = rng.NextDouble() * 10.0 - 5.0
                let mass = rng.NextDouble() * 49.9 + 0.1
                let isSphere = rng.Next(2) = 0
                if isSphere then
                    let radius = rng.NextDouble() * 0.45 + 0.05
                    let id = nextId "sphere"
                    let result = addSphere connection (x, y, z) radius mass id
                    sb.AppendLine($"  {id}: {result}") |> ignore
                else
                    let hx = rng.NextDouble() * 0.45 + 0.05
                    let hy = rng.NextDouble() * 0.45 + 0.05
                    let hz = rng.NextDouble() * 0.45 + 0.05
                    let id = nextId "box"
                    let result = addBox connection (x, y, z) (hx, hy, hz) mass id
                    sb.AppendLine($"  {id}: {result}") |> ignore
            sb.ToString()

    /// <summary>Generates a vertical stack of unit-sized crates at the specified base position, each spaced 1 unit apart vertically.</summary>
    [<McpServerTool>]
    [<Description("Generate a vertical stack of unit-sized crates (0.5 half-extents, 20 kg mass) spaced 1 unit apart vertically")>]
    static member generate_stack(connection: GrpcConnection,
                                  [<Description("Number of crates in the stack")>] count: int,
                                  [<Description("Base X position. Default: 0.")>] x: float,
                                  [<Description("Base Y position. Default: 0.")>] y: float,
                                  [<Description("Base Z position. Default: 0.")>] z: float) =
        if count <= 0 then "Error: count must be greater than 0"
        else
            let sb = StringBuilder()
            sb.AppendLine($"Generated stack of {count} crates:") |> ignore
            for i in 0..(count - 1) do
                let cy = y + (float i) * 1.0 + 0.5
                let id = nextId "crate"
                let result = addBox connection (x, cy, z) (0.5, 0.5, 0.5) 20.0 id
                sb.AppendLine($"  {id}: {result}") |> ignore
            sb.ToString()

    /// <summary>Generates a horizontal row of spheres along the X axis with configurable spacing.</summary>
    [<McpServerTool>]
    [<Description("Generate a horizontal row of spheres along the X axis (0.2 radius, 1.0 kg mass)")>]
    static member generate_row(connection: GrpcConnection,
                                [<Description("Number of spheres in the row")>] count: int,
                                [<Description("Start X position. Default: 0.")>] x: float,
                                [<Description("Y position. Default: 0.")>] y: float,
                                [<Description("Z position. Default: 0.")>] z: float,
                                [<Description("Spacing between sphere centers. Default: 0.5. Values <= 0 are treated as 0.5.")>] spacing: Nullable<float>) =
        if count <= 0 then "Error: count must be greater than 0"
        else
            let spacingVal = if spacing.HasValue then spacing.Value else 0.5
            let sp = if spacingVal <= 0.0 then 0.5 else spacingVal
            let sb = StringBuilder()
            sb.AppendLine($"Generated row of {count} spheres:") |> ignore
            for i in 0..(count - 1) do
                let cx = x + (float i) * sp
                let id = nextId "sphere"
                let result = addSphere connection (cx, y, z) 0.2 1.0 id
                sb.AppendLine($"  {id}: {result}") |> ignore
            sb.ToString()

    /// <summary>Generates a 2D grid of crates on the XZ plane with 1-unit spacing between centers.</summary>
    [<McpServerTool>]
    [<Description("Generate a 2D grid of crates on the XZ plane with 1-unit spacing (0.5 half-extents, 20 kg mass)")>]
    static member generate_grid(connection: GrpcConnection,
                                 [<Description("Number of rows")>] rows: int,
                                 [<Description("Number of columns")>] cols: int,
                                 [<Description("Start X position. Default: 0.")>] x: float,
                                 [<Description("Y position. Default: 0.5. Values <= 0 are treated as 0.5.")>] y: float,
                                 [<Description("Start Z position. Default: 0.")>] z: float) =
        if rows <= 0 || cols <= 0 then "Error: rows and cols must be greater than 0"
        else
            let sb = StringBuilder()
            sb.AppendLine($"Generated {rows}x{cols} grid:") |> ignore
            for r in 0..(rows - 1) do
                for c in 0..(cols - 1) do
                    let cx = x + (float c) * 1.0
                    let cz = z + (float r) * 1.0
                    let cy = if y <= 0.0 then 0.5 else y
                    let id = nextId "crate"
                    let result = addBox connection (cx, cy, cz) (0.5, 0.5, 0.5) 20.0 id
                    sb.AppendLine($"  {id}: {result}") |> ignore
            sb.ToString()

    /// <summary>Generates a pyramid of crates with the widest layer at the base, narrowing by one crate per layer.</summary>
    [<McpServerTool>]
    [<Description("Generate a pyramid of crates with the widest layer at the base, narrowing by one crate per layer (0.5 half-extents, 20 kg mass)")>]
    static member generate_pyramid(connection: GrpcConnection,
                                    [<Description("Number of layers")>] layers: int,
                                    [<Description("Base X position. Default: 0.")>] x: float,
                                    [<Description("Base Y position. Default: 0.")>] y: float,
                                    [<Description("Base Z position. Default: 0.")>] z: float) =
        if layers <= 0 then "Error: layers must be greater than 0"
        else
            let sb = StringBuilder()
            let mutable total = 0
            for i in 0..(layers - 1) do
                let width = layers - i
                let offsetX = (float i) * 0.5
                let cy = y + (float i) * 1.0 + 0.5
                for j in 0..(width - 1) do
                    let cx = x + offsetX + (float j) * 1.0
                    let id = nextId "crate"
                    let _ = addBox connection (cx, cy, z) (0.5, 0.5, 0.5) 20.0 id
                    total <- total + 1
            sb.AppendLine($"Generated pyramid with {layers} layers ({total} crates)") |> ignore
            sb.ToString()
