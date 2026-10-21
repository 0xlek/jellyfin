# Media server (Jellyfin-based)

A media server built from a fork of Jellyfin 12.1. It is a separate project: not affiliated with or
supported by the Jellyfin project, so report problems here rather than upstream.

What differs from Jellyfin:

- Seek-bar previews (trickplay) are generated in one ffmpeg pass from key frames by default, with the
  frame timing fixed for files whose frame rate is mislabeled.
- Generated tiles are tracked against the source file, published atomically and served with
  versioned, immutable caching.

## Run

```
docker run --network host -v ./config:/config -v ./cache:/cache -v /path/to/media:/media ghcr.io/0xlek/jellyfin:latest
```

Pair it with the web client from https://github.com/0xlek/jellyfin-web.

## Build and test

```
dotnet build
dotnet test
```

## License

GPL-2.0, see [LICENSE](LICENSE). Based on a fork of Jellyfin; earlier contributors are listed in
[CONTRIBUTORS.md](CONTRIBUTORS.md).
