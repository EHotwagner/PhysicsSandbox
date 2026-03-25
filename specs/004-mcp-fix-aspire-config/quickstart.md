# Quickstart: MCP Tool Schema Fix & Aspire MCP Configuration

**Branch**: `004-mcp-fix-aspire-config` | **Date**: 2026-03-25

## Prerequisites

- .NET 10.0 SDK
- Aspire CLI (`dotnet tool list -g` should show `aspire-cli`)
- Running PhysicsSandbox Aspire stack (`./start.sh`)

## Build & Test

```bash
# Build solution (headless)
dotnet build PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run all tests including new MCP regression tests
dotnet test PhysicsSandbox.slnx -p:StrideCompilerSkipBuild=true

# Run only MCP integration tests
dotnet test tests/PhysicsSandbox.Integration.Tests -p:StrideCompilerSkipBuild=true --filter "ClassName~Mcp"
```

## Verify MCP Tool Fix

```bash
# Start the stack
./start.sh

# Test with Python runner (existing tool)
python3 reports/mcp_test_runner.py

# Expected: 59/59 tools OK (was 42/59 before fix)
```

## Verify Aspire Dashboard MCP

```bash
# With stack running, launch Claude Code
claude

# In Claude Code, Aspire tools should be available:
# - list_resources
# - list_console_logs
# - doctor
# - search_docs
# - list_docs
```

## Key Files Changed

| File | Change |
|------|--------|
| `src/PhysicsSandbox.Mcp/SimulationTools.fs` | Optional params: `?param` → `Nullable<T>`, improved descriptions |
| `src/PhysicsSandbox.Mcp/GeneratorTools.fs` | Make seed/spacing optional |
| `src/PhysicsSandbox.Mcp/RecordingTools.fs` | Already uses `?param`, convert to Nullable |
| `src/PhysicsSandbox.Mcp/RecordingQueryTools.fs` | Make pagination/time params optional |
| `src/PhysicsSandbox.Mcp/MeshFetchQueryTools.fs` | Make filter/pagination params optional |
| `src/PhysicsSandbox.Mcp/StressTestTools.fs` | Convert optional params to Nullable |
| `src/PhysicsSandbox.Mcp/*.fsi` | Update signatures to match |
| `tests/PhysicsSandbox.Integration.Tests/` | New MCP regression test class |
| `.mcp.json` | Add aspire-dashboard stdio entry |
