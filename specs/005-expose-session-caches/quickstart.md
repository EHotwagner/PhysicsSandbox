# Quickstart: Expose Session Caches

**Feature**: 005-expose-session-caches | **Date**: 2026-03-29

## What This Feature Does

Exposes four previously-internal Session fields as public accessor functions, allowing code outside the PhysicsClient assembly to read body property caches, constraint lists, registered shape lists, and the server address.

## Usage

```fsharp
open PhysicsClient

// After connecting
let session = Session.connect "http://localhost:5180" |> Result.defaultWith failwith

// Read body properties cache (mass, shape, color, motion type per body)
let props = Session.bodyPropertiesCache session
for kvp in props do
    printfn $"Body {kvp.Key}: mass={kvp.Value.Mass}, static={kvp.Value.IsStatic}"

// Read active constraints
let constraints = Session.cachedConstraints session
printfn $"Active constraints: {constraints.Length}"

// Read registered shapes
let shapes = Session.cachedRegisteredShapes session
printfn $"Registered shapes: {shapes.Length}"

// Read server address (diagnostics)
let addr = Session.serverAddress session
printfn $"Connected to: {addr}"
```

## Implementation Scope

- 4 new accessor functions in `Session.fs` / `Session.fsi`
- Surface area baseline test update in `SurfaceAreaTests.fs`
- No new files, no new dependencies, no behavior changes
