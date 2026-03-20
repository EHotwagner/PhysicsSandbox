# MCP Tool Contracts

**Date**: 2026-03-20 | **Feature**: 005-mcp-server-testing

## Transport

- Protocol: MCP (Model Context Protocol) over stdio (JSON-RPC 2.0)
- Server name: `physics-sandbox`
- Launch: `dotnet run --project src/PhysicsSandbox.Mcp [server-address]`
- Default server address: `https://localhost:7180`

## Tool Schemas

### Simulation Commands

#### add_body
```json
{
  "name": "add_body",
  "description": "Add a rigid body to the physics simulation.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "shape": { "type": "string", "enum": ["sphere", "box"], "description": "Body shape type" },
      "radius": { "type": "number", "description": "Sphere radius (required if shape=sphere)" },
      "half_extents_x": { "type": "number", "description": "Box half-extent X (required if shape=box)" },
      "half_extents_y": { "type": "number", "description": "Box half-extent Y (required if shape=box)" },
      "half_extents_z": { "type": "number", "description": "Box half-extent Z (required if shape=box)" },
      "x": { "type": "number", "description": "Position X", "default": 0 },
      "y": { "type": "number", "description": "Position Y", "default": 5 },
      "z": { "type": "number", "description": "Position Z", "default": 0 },
      "mass": { "type": "number", "description": "Body mass (0 = static)", "default": 1 }
    },
    "required": ["shape"]
  }
}
```

#### apply_force
```json
{
  "name": "apply_force",
  "description": "Apply a continuous force to a body.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "body_id": { "type": "string", "description": "Target body ID" },
      "x": { "type": "number", "description": "Force X component", "default": 0 },
      "y": { "type": "number", "description": "Force Y component", "default": 0 },
      "z": { "type": "number", "description": "Force Z component", "default": 0 }
    },
    "required": ["body_id"]
  }
}
```

#### apply_impulse
```json
{
  "name": "apply_impulse",
  "description": "Apply an instantaneous impulse to a body.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "body_id": { "type": "string", "description": "Target body ID" },
      "x": { "type": "number", "description": "Impulse X component", "default": 0 },
      "y": { "type": "number", "description": "Impulse Y component", "default": 0 },
      "z": { "type": "number", "description": "Impulse Z component", "default": 0 }
    },
    "required": ["body_id"]
  }
}
```

#### apply_torque
```json
{
  "name": "apply_torque",
  "description": "Apply a torque to a body.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "body_id": { "type": "string", "description": "Target body ID" },
      "x": { "type": "number", "description": "Torque X component", "default": 0 },
      "y": { "type": "number", "description": "Torque Y component", "default": 0 },
      "z": { "type": "number", "description": "Torque Z component", "default": 0 }
    },
    "required": ["body_id"]
  }
}
```

#### set_gravity
```json
{
  "name": "set_gravity",
  "description": "Set the global gravity vector.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "x": { "type": "number", "default": 0 },
      "y": { "type": "number", "default": -9.81 },
      "z": { "type": "number", "default": 0 }
    }
  }
}
```

#### step
```json
{
  "name": "step",
  "description": "Advance the simulation by one time step.",
  "inputSchema": { "type": "object", "properties": {} }
}
```

#### play
```json
{
  "name": "play",
  "description": "Start continuous simulation.",
  "inputSchema": { "type": "object", "properties": {} }
}
```

#### pause
```json
{
  "name": "pause",
  "description": "Pause the simulation.",
  "inputSchema": { "type": "object", "properties": {} }
}
```

#### remove_body
```json
{
  "name": "remove_body",
  "description": "Remove a body from the simulation.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "body_id": { "type": "string", "description": "Body ID to remove" }
    },
    "required": ["body_id"]
  }
}
```

#### clear_forces
```json
{
  "name": "clear_forces",
  "description": "Clear all forces on a body.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "body_id": { "type": "string", "description": "Body ID" }
    },
    "required": ["body_id"]
  }
}
```

### View Commands

#### set_camera
```json
{
  "name": "set_camera",
  "description": "Set the 3D viewer camera position and target.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "pos_x": { "type": "number", "default": 0 },
      "pos_y": { "type": "number", "default": 10 },
      "pos_z": { "type": "number", "default": 20 },
      "target_x": { "type": "number", "default": 0 },
      "target_y": { "type": "number", "default": 0 },
      "target_z": { "type": "number", "default": 0 }
    }
  }
}
```

#### set_zoom
```json
{
  "name": "set_zoom",
  "description": "Set the 3D viewer zoom level.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "level": { "type": "number", "description": "Zoom level (1.0 = default)" }
    },
    "required": ["level"]
  }
}
```

#### toggle_wireframe
```json
{
  "name": "toggle_wireframe",
  "description": "Toggle wireframe rendering mode in the 3D viewer.",
  "inputSchema": { "type": "object", "properties": {} }
}
```

### Query Tools

#### get_state
```json
{
  "name": "get_state",
  "description": "Get the current simulation state (bodies, time, running status). Returns cached data from background stream.",
  "inputSchema": { "type": "object", "properties": {} }
}
```

Response format (text):
```
Simulation State (cached 0.3s ago)
Time: 12.450s | Running: true | Bodies: 3

  ID            | Position          | Velocity          | Mass  | Shape
  bold-falcon   | (0.0, 4.2, 0.0)  | (0.0, -3.1, 0.0) | 1.0   | Sphere(r=0.5)
  red-tiger     | (2.0, 0.5, 0.0)  | (0.0, 0.0, 0.0)  | 5.0   | Box(1x1x1)
  swift-eagle   | (-1.0, 8.0, 3.0) | (0.0, -1.2, 0.0) | 2.0   | Sphere(r=1.0)
```

#### get_status
```json
{
  "name": "get_status",
  "description": "Get MCP server connection status and health.",
  "inputSchema": { "type": "object", "properties": {} }
}
```

Response format (text):
```
MCP Server Status
Server: https://localhost:7180
State Stream: connected (last update 0.3s ago)
Simulation: connected
```
