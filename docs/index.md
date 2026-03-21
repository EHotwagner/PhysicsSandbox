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

Four services communicate through a central gRPC hub:

```
                ┌──────────────┐
                │    Server    │
                │   (Hub/API)  │
                └──┬──┬──┬────┘
        commands   │  │  │  sim data + UI
        ┌──────────┘  │  └──────────┐
        ▼             │             ▼
┌──────────────┐      │      ┌──────────────┐
│  Simulation  │      │      │   3D Viewer  │
│  (Physics)   │      │      │   (Render)   │
└──────────────┘      │      └──────────────┘
                      │
                ┌─────┴────────┐
                │  REPL Client │
                │  (Commands)  │
                └──────────────┘
```

## Documentation

- **[Getting Started](getting-started.html)** — Build, run, and interact with the sandbox
- **[Architecture](architecture.html)** — Service design, data flow, and gRPC contracts
- **[Demo Scripts](demo-scripts.html)** — 15 physics demos in F# and Python
- **[MCP Tools](mcp-tools.html)** — 38 tools for AI-assisted physics debugging
- **[Known Issues](known-issues.html)** — Current limitations and workarounds
- **[Test Suite](tests.html)** — 165 tests across 5 projects
