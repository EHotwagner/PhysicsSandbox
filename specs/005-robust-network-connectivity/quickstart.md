# Quickstart: 005-robust-network-connectivity

**Date**: 2026-03-24

## Prerequisites

1. Rebase branch on `main` to inherit 004-camera-smooth-demos fixes:
   ```bash
   git rebase main
   ```

2. Build the solution:
   ```bash
   dotnet build PhysicsSandbox.slnx
   ```

## Verification Steps

### 1. ViewCommand Broadcast (US1)

```bash
# Start the Aspire stack
./start.sh --http

# In a separate terminal, run a demo that sends rapid ViewCommands:
dotnet fsi Scripting/demos/Demo22_CameraShowcase.fsx

# Verify: All camera moves and narration labels appear in the viewer
# Verify: No commands are dropped (check Aspire structured logs for ViewCmd RECV entries)
```

**Multi-viewer broadcast test**:
```bash
# Start two viewer instances (requires manual launch of second viewer)
# Run a demo — both viewers should receive every command
# Kill one viewer — the other continues receiving
```

### 2. MCP SSE Connectivity (US2)

```bash
# With Aspire stack running, test MCP endpoint:
curl -N http://localhost:5199/sse

# Should receive SSE event stream (not HTTP/2 error)
# If using DCP proxy port, verify it also works
```

### 3. Process Cleanup (US3)

```bash
# Start full stack, then:
./kill.sh && echo "alive"

# Verify: prints "alive" (shell not killed)
# Verify: no PhysicsSandbox processes remain (ps aux | grep PhysicsSandbox)
```

### 4. NetworkProblems.md (US4)

```bash
# Verify consolidated entries:
cat reports/NetworkProblems.md

# Should contain:
# - Environment section with port table
# - All 6+ documented issues with structured format
```

### 5. Camera Mode Resilience (US5)

```bash
# Run Demo22 — camera follow/orbit/chase modes should work on newly-created bodies
# No silent cancellation of body-relative modes
```

### 6. Run Tests

```bash
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true
```
