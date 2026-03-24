# Data Model: Enhance Demos with New Shapes and Viewer Labels

**Date**: 2026-03-24

## Entities

### DemoMetadata (new proto message)

| Field       | Type   | Description                                    |
|-------------|--------|------------------------------------------------|
| name        | string | Short demo name (e.g., "Hello Drop")           |
| description | string | One-line description (max ~80 chars recommended)|

**Lifecycle**: Set once per demo run. Cleared on simulation reset. Persists until next SetDemoMetadata command or reset.

**Relationships**: Carried inside ViewCommand as a new oneof variant. Received by PhysicsViewer via StreamViewCommands.

### ViewCommand (extended)

Existing oneof with new variant:

| Variant              | Field # | Description                          |
|----------------------|---------|--------------------------------------|
| set_camera           | 1       | Existing: camera position/target     |
| toggle_wireframe     | 2       | Existing: wireframe on/off           |
| set_zoom             | 3       | Existing: camera zoom level          |
| set_demo_metadata    | 4       | **New**: demo name + description     |

### SceneState (viewer, extended)

| Field           | Type            | Description                          |
|-----------------|-----------------|--------------------------------------|
| Bodies          | Map<string, Entity> | Existing                         |
| Placeholders    | Set<string>     | Existing                             |
| SimTime         | float           | Existing                             |
| SimRunning      | bool            | Existing                             |
| Wireframe       | bool            | Existing                             |
| DemoName        | string option   | **New**: current demo name           |
| DemoDescription | string option   | **New**: current demo description    |

**State transitions**:
- Initial: `DemoName = None, DemoDescription = None`
- On SetDemoMetadata: `DemoName = Some name, DemoDescription = Some description`
- On simulation reset (no new metadata): remains as-is until next SetDemoMetadata or explicit clear

## No New Persistent Storage

All data is in-memory and transient. No database, file, or cache changes required.
