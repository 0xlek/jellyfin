# syntax=docker/dockerfile:1
# The official 12.1 image with this fork's server build swapped in; ffmpeg and the bundled web client stay as shipped.

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
ARG TARGETARCH
WORKDIR /src
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Jellyfin.Server/Jellyfin.Server.csproj \
        --configuration Release \
        --runtime linux-$([ "$TARGETARCH" = arm64 ] && echo arm64 || echo x64) \
        --self-contained \
        -p:DebugSymbols=false -p:DebugType=none \
        --output /server

FROM jellyfin/jellyfin:12.1@sha256:78d3ea1207d1322471fcac39a614f004f2ccf7e878f95ab2977d752f07e4dd7e
RUN find /jellyfin -maxdepth 1 -type f -delete
COPY --from=build /server/ /jellyfin/
