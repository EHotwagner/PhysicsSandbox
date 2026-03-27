#!/usr/bin/env bash
set -euo pipefail

# Compile Stride assets (requires GPU — skipped during image build)
echo "Compiling Stride assets…"
dotnet /root/.nuget/packages/stride.core.assets.compilerapp/4.3.0.2507/lib/net10.0/Stride.Core.Assets.CompilerApp.dll \
    --disable-auto-compile \
    --project-configuration Release \
    --platform=Linux \
    --compile-property:StrideGraphicsApi=OpenGL \
    --output-path="/src/src/PhysicsViewer/bin/Release/net10.0/linux-x64/data" \
    --build-path="/src/src/PhysicsViewer/obj/stride/assetbuild/data" \
    --package-file="/src/src/PhysicsViewer/PhysicsViewer.fsproj"
echo "Stride assets compiled."

exec dotnet run --project src/PhysicsSandbox.AppHost --no-build -c Release --launch-profile http
