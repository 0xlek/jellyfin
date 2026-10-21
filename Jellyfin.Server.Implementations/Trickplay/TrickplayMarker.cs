namespace Jellyfin.Server.Implementations.Trickplay;

/// <summary>
/// The contents of a trickplay marker file.
/// </summary>
/// <param name="Source">The source fingerprint the tiles were made from.</param>
/// <param name="Version">A value that changes whenever the tiles are regenerated.</param>
internal sealed record TrickplayMarker(string Source, string Version);
