# PhysicsSandbox — GPU-accelerated physics sandbox with Aspire orchestration
#
# Build:  podman build -t physicssandbox .
# Run:    podman run --rm -it \
#           --device /dev/dri \
#           --network host \
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

# FreeImage symlink expected by Stride
RUN ln -sf /usr/lib/x86_64-linux-gnu/libfreeimage.so.3 /usr/lib/freeimage.so

# Clone and pack BepuFSharp into local NuGet feed
RUN git clone https://github.com/EHotwagner/BepuFSharp.git /tmp/BepuFSharp \
    && dotnet pack /tmp/BepuFSharp -c Release -o /nuget-local -p:NoWarn=NU5104 \
    && rm -rf /tmp/BepuFSharp

# Clone PhysicsSandbox
WORKDIR /src
RUN git clone https://github.com/EHotwagner/PhysicsSandbox.git .

# Point NuGet at the local BepuFSharp feed
RUN dotnet nuget add source /nuget-local -n bepufsharp

# Build
RUN dotnet build PhysicsSandbox.slnx -c Release

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production \
    DISPLAY=:0

EXPOSE 8081 5199

ENTRYPOINT ["dotnet", "run", "--project", "src/PhysicsSandbox.AppHost", "--no-build", "-c", "Release", "--launch-profile", "http"]
