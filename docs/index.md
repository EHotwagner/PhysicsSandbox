---
title: Physics Sandbox
category: Overview
categoryindex: 1
index: 1
description: A real-time physics simulation built with F# microservices, .NET Aspire, and BepuPhysics2.
---

# Physics Sandbox

A real-time 3D physics simulation built as an F# microservices architecture, orchestrated by .NET Aspire. Drop bodies, apply forces, flip gravity, and watch the results in a live 3D viewer — all controlled through a REPL client, scripting (F# and Python), or AI assistants via MCP.

## Architecture

Five services communicate through a central gRPC hub:

<pre class="mermaid">
graph TD
    Server["Server\n(gRPC Hub)"]
    Sim["Simulation\n(BepuPhysics2)"]
    Viewer["3D Viewer\n(Stride3D)"]
    Client["REPL Client\n(Spectre.Console)"]
    MCP["MCP Server\n(38 AI Tools)"]
    Server <-->|"commands / state\nbidirectional stream"| Sim
    Client -->|"commands\nview commands"| Server
    Server -->|"state stream"| Client
    Server -->|"state stream\nview commands"| Viewer
    MCP -->|"commands"| Server
    Server -->|"state stream"| MCP
</pre>

## Documentation

- **[Getting Started](getting-started.html)** — Build, run, and interact with the sandbox
- **[Architecture](architecture.html)** — Service design, data flow, and gRPC contracts
- **[Scripting Library](scripting-library.html)** — Convenience library for F# script authors
- **[Demo Scripts](demo-scripts.html)** — 15 physics demos in F# and Python
- **[MCP Tools](mcp-tools.html)** — 38 tools for AI-assisted physics debugging
- **[Known Issues](known-issues.html)** — Current limitations and workarounds
- **[Test Suite](tests.html)** — 165 tests across 5 projects
- **[API Reference](reference/index.html)** — PhysicsSandbox.Scripting module documentation
