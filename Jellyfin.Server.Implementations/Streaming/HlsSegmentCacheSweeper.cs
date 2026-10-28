using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Streaming;

/// <summary>
/// Removes the cached segments of HLS streams nobody has watched for <see cref="HlsSegmentCache.TimeToLive"/>, and the
/// least recently watched ones first when the cache outgrows half its volume or 50 GB.
/// </summary>
public sealed class HlsSegmentCacheSweeper : BackgroundService
{
    private const long MaxCacheBytes = 50L * 1024 * 1024 * 1024;

    private static readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    private readonly IServerConfigurationManager _configurationManager;
    private readonly ITranscodeManager _transcodeManager;
    private readonly ILogger<HlsSegmentCacheSweeper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HlsSegmentCacheSweeper"/> class.
    /// </summary>
    /// <param name="configurationManager">The <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="transcodeManager">The <see cref="ITranscodeManager"/>.</param>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
    public HlsSegmentCacheSweeper(IServerConfigurationManager configurationManager, ITranscodeManager transcodeManager, ILogger<HlsSegmentCacheSweeper> logger)
    {
        _configurationManager = configurationManager;
        _transcodeManager = transcodeManager;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        do
        {
            try
            {
                Sweep(DateTime.UtcNow);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Sweeping the HLS segment cache failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private void Sweep(DateTime now)
    {
        var directory = _configurationManager.GetTranscodePath();
        if (!Directory.Exists(directory))
        {
            return;
        }

        var streams = new DirectoryInfo(directory).EnumerateFiles()
            .Where(f => f.Name.Length >= 32 && IsHex(f.Name.AsSpan(0, 32)))
            .GroupBy(f => f.Name[..32], StringComparer.OrdinalIgnoreCase)
            // Only HLS streams; progressive outputs are removed when their job ends
            .Where(g => g.Any(f => f.Extension is ".m3u8" or HlsSegmentCache.MarkerExtension))
            .Select(g => new CachedStream(g.Key, g.ToList(), g.Max(f => f.LastWriteTimeUtc)))
            .Where(s => !IsActive(directory, s.Name))
            .OrderBy(s => s.LastUsed)
            .ToList();

        var total = streams.Sum(s => s.Bytes);
        var cap = Math.Min(new DriveInfo(directory).TotalSize / 2, MaxCacheBytes);
        foreach (var stream in streams)
        {
            var expired = now - stream.LastUsed > HlsSegmentCache.TimeToLive;
            if (!expired && total <= cap)
            {
                break;
            }

            _logger.LogInformation(
                "Removing cached HLS stream {Name}: {Count} files, {Megabytes} MB, last served {LastUsed:u} ({Reason})",
                stream.Name,
                stream.Files.Count,
                stream.Bytes / (1024 * 1024),
                stream.LastUsed,
                expired ? "expired" : "cache full");

            foreach (var file in stream.Files)
            {
                try
                {
                    file.Delete();
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Could not delete {Path}", file.FullName);
                }
            }

            total -= stream.Bytes;
        }
    }

    private bool IsActive(string directory, string name)
        => _transcodeManager.GetTranscodingJob(Path.Combine(directory, name + ".m3u8"), TranscodingJobType.Hls) is { HasExited: false };

    private static bool IsHex(ReadOnlySpan<char> value)
    {
        foreach (var c in value)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private sealed record CachedStream(string Name, List<FileInfo> Files, DateTime LastUsed)
    {
        public long Bytes { get; } = Files.Sum(f => f.Length);
    }
}
