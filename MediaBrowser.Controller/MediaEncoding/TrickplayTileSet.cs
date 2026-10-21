namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Trickplay tiles written by one extraction run.
/// </summary>
/// <param name="Directory">The temporary directory holding the tiles, named 0.jpg, 1.jpg, ...</param>
/// <param name="ThumbnailCount">The number of thumbnails across all tiles; the last tile is padded.</param>
public record TrickplayTileSet(string Directory, int ThumbnailCount);
