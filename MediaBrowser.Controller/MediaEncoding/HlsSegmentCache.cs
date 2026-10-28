using System;
using System.IO;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// HLS segments stay on disk after playback stops, so seeking back, resuming or reloading serves them again without
/// restarting ffmpeg. A stream's segments are removed once none of them has been served for <see cref="TimeToLive"/>.
/// </summary>
public static class HlsSegmentCache
{
    /// <summary>
    /// The extension of the marker file whose modification time is when a segment of the stream was last served.
    /// </summary>
    public const string MarkerExtension = ".last";

    /// <summary>
    /// How long a stream's segments are kept after the last one was served.
    /// </summary>
    public static readonly TimeSpan TimeToLive = TimeSpan.FromHours(1);

    /// <summary>
    /// Records that a segment of the stream was just served.
    /// </summary>
    /// <param name="playlistPath">The path of the stream's playlist.</param>
    public static void Touch(string playlistPath)
    {
        var marker = Path.ChangeExtension(playlistPath, MarkerExtension);
        try
        {
            if (!File.Exists(marker))
            {
                File.Create(marker).Dispose();
            }

            File.SetLastWriteTimeUtc(marker, DateTime.UtcNow);
        }
        catch (IOException)
        {
            // A missed touch only shortens how long the segments are kept
        }
    }
}
