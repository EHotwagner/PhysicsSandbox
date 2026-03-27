# Quickstart: Fix Container Build Scripts

## Prerequisites

- .NET 10 SDK
- Python 3.10+ with grpcio-tools
- Running PhysicsSandbox stack (for end-to-end verification)

## Verification Steps

### NuGet Config Fix (Part 1)

```bash
# Verify no NU1301 error when running demo script on dev workstation
cd /home/developer/projects/PhysicsSandbox
dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx
# Expected: script loads and runs (may fail to connect if server not running, but no NU1301)
```

### Python Stub Fix (Part 2)

```bash
# Regenerate stubs (if needed)
cd /home/developer/projects/PhysicsSandbox/Scripting/demos_py
bash generate_stubs.sh

# Verify relative import in generated file
grep "from \. import physics_hub_pb2" generated/physics_hub_pb2_grpc.py
# Expected: "from . import physics_hub_pb2 as physics__hub__pb2"

# Verify no bare import
grep "^import physics_hub_pb2" generated/physics_hub_pb2_grpc.py
# Expected: no output

# Test Python imports without PYTHONPATH
cd /home/developer/projects/PhysicsSandbox
PYTHONPATH= python3 -c "
import sys; sys.path.insert(0, '.')
from Scripting.demos_py.generated import physics_hub_pb2 as pb
from Scripting.demos_py.generated import physics_hub_pb2_grpc as pb_grpc
print('OK')
"
# Expected: "OK"
```

### Container Build Verification

```bash
# Build container
podman build -t physics-sandbox .

# Run and test F# demos
podman run --rm physics-sandbox dotnet fsi Scripting/demos/Demo01_HelloDrop.fsx

# Run and test Python demos (PYTHONPATH should NOT be set in container)
podman run --rm physics-sandbox python3 Scripting/demos_py/01_hello_physics.py
```
