#!/usr/bin/env bash
# Generate Python gRPC stubs from physics_hub.proto
# Usage: cd Scripting/demos_py && bash generate_stubs.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROTO_DIR="$SCRIPT_DIR/../../src/PhysicsSandbox.Shared.Contracts/Protos"
OUT_DIR="$SCRIPT_DIR/generated"

if [ ! -f "$PROTO_DIR/physics_hub.proto" ]; then
    echo "ERROR: Proto file not found at $PROTO_DIR/physics_hub.proto"
    exit 1
fi

echo "Generating Python gRPC stubs..."
python3 -m grpc_tools.protoc \
    --proto_path="$PROTO_DIR" \
    --python_out="$OUT_DIR" \
    --grpc_python_out="$OUT_DIR" \
    physics_hub.proto

# Fix the import path in the generated gRPC stub
# grpc_tools generates: import physics_hub_pb2 as ...
# But we need: from . import physics_hub_pb2 as ... (relative import)
sed -i 's/^import physics_hub_pb2 as/from . import physics_hub_pb2 as/' "$OUT_DIR/physics_hub_pb2_grpc.py"

echo "Generated:"
echo "  $OUT_DIR/physics_hub_pb2.py"
echo "  $OUT_DIR/physics_hub_pb2_grpc.py"
echo "Done."
