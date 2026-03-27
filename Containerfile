# PhysicsSandbox — GPU-accelerated physics sandbox with Aspire orchestration
#
# Build:  podman build -t physicssandbox .
# Run:    podman run --rm -it \
#           --device nvidia.com/gpu=all \
#           -e DISPLAY=$DISPLAY \
#           -v /tmp/.X11-unix:/tmp/.X11-unix \
#           physicssandbox
#
# Dashboard: http://localhost:8081  |  MCP: http://localhost:5199/sse

FROM mcr.microsoft.com/dotnet/sdk:10.0

# System dependencies for Stride3D viewer
RUN apt-get update && apt-get install -y --no-install-recommends \
        git \
        libopenal1 \
        libfreetype6 \
        libsdl2-2.0-0 \
        libfreeimage3 \
        fonts-liberation \
        mesa-vulkan-drivers \
        libgl1 \
        libgles2 \
    && rm -rf /var/lib/apt/lists/*

# Native library symlinks expected by Stride
RUN ln -sf /usr/lib/x86_64-linux-gnu/libfreeimage.so.3 /usr/lib/freeimage.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/libfreetype.so.6 /usr/lib/libfreetype.so \
    && ln -sf /usr/lib/x86_64-linux-gnu/libfreetype.so.6 /usr/lib/freetype.so

# Clone PhysicsSandbox
WORKDIR /src
RUN git clone https://github.com/EHotwagner/PhysicsSandbox.git .

# Clone and pack BPEWrapper (as BepuFSharp 0.3.0) into local-packages feed
RUN git clone https://github.com/EHotwagner/BPEWrapper.git /tmp/BPEWrapper \
    && dotnet pack /tmp/BPEWrapper -c Release -o /src/local-packages -p:NoWarn=NU5104 -p:Version=0.3.0 \
    && rm -rf /tmp/BPEWrapper

# Pack in-solution projects into local-packages (transitive PackageReference chain from Mcp)
RUN dotnet pack src/PhysicsSandbox.ServiceDefaults -c Release -o /src/local-packages \
    && dotnet pack src/PhysicsSandbox.Shared.Contracts -c Release -o /src/local-packages \
    && dotnet pack src/PhysicsClient -c Release -o /src/local-packages \
    && dotnet pack src/PhysicsSandbox.Scripting -c Release -o /src/local-packages

# Build (skip Stride asset compile — needs GPU, runs at container start via entrypoint)
RUN dotnet build PhysicsSandbox.slnx -c Release -p:StrideCompilerSkipBuild=true

COPY entrypoint.sh /src/entrypoint.sh

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production \
    DISPLAY=:0

EXPOSE 8081 5199

ENTRYPOINT ["/src/entrypoint.sh"]
