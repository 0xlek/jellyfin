using System;

namespace Jellyfin.MediaEncoding.Hls.Playlist;

/// <summary>
/// Generator for dynamic HLS playlists where the segment lengths aren't known in advance.
/// </summary>
public interface IDynamicHlsPlaylistGenerator
{
    /// <summary>
    /// Creates the main playlist containing the main video or audio stream.
    /// </summary>
    /// <param name="request">An instance of the <see cref="CreateMainPlaylistRequest"/> class.</param>
    /// <returns>The playlist as a formatted string.</returns>
    string CreateMainPlaylist(CreateMainPlaylistRequest request);

    /// <summary>
    /// Gets whether a remux playlist for the file is cut at its keyframes, so ffmpeg has to cut at every keyframe too.
    /// </summary>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="filePath">The path to the media file.</param>
    /// <returns><c>true</c> when the playlist has a segment per keyframe.</returns>
    bool HasKeyframeSegments(Guid mediaSourceId, string filePath);
}
