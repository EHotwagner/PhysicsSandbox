#!/usr/bin/env bash
set -euo pipefail

# Compile Stride assets (requires GPU — skipped during image build)
echo "Compiling Stride assets…"
STRIDE_COMPILER=$(find /root/.nuget/packages/stride.core.assets.compilerapp -name 'Stride.Core.Assets.CompilerApp.dll' -path '*/net10.0/*' | head -1)
if [ -z "$STRIDE_COMPILER" ]; then
    echo "ERROR: Stride asset compiler not found" >&2
    exit 1
fi
dotnet "$STRIDE_COMPILER" \
    --disable-auto-compile \
    --project-configuration Release \
    --platform=Linux \
    --compile-property:StrideGraphicsApi=OpenGL \
    --output-path="/src/src/PhysicsViewer/bin/Release/net10.0/linux-x64/data" \
    --build-path="/src/src/PhysicsViewer/obj/stride/assetbuild/data" \
    --package-file="/src/src/PhysicsViewer/PhysicsViewer.fsproj"
echo "Stride assets compiled."

exec dotnet run --project src/PhysicsSandbox.AppHost --no-build -c Release --launch-profile http
