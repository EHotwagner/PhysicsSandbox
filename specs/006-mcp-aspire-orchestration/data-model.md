# Data Model: Add MCP Server to Aspire AppHost Orchestration

No new entities are introduced by this feature.

The MCP server is an existing project being added to the Aspire resource graph. The only data change is the addition of a new resource node in the Aspire orchestration:

## Resource Graph (updated)

```
server (PhysicsServer) ─────────────────────────────┐
  ├── simulation (PhysicsSimulation) [WithReference] │
  ├── viewer (PhysicsViewer) [WithReference]          │
  ├── client (PhysicsClient) [WithReference]          │
  └── mcp (PhysicsSandbox.Mcp) [WithReference] ← NEW │
```

All edges represent `WithReference` + `WaitFor` relationships, meaning the child resource receives the parent's endpoint via environment variables and waits for it to be healthy before starting.
