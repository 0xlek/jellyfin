using System;

namespace MediaBrowser.Model.Dto;

/// <summary>
/// One device's progress through an item.
/// </summary>
public class WatchSessionDto
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public string? Client { get; set; }

    /// <summary>
    /// Gets or sets the furthest position reached, in ticks.
    /// </summary>
    public long PositionTicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item was watched to the end.
    /// </summary>
    public bool IsFinished { get; set; }

    /// <summary>
    /// Gets or sets when the session started.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Gets or sets when the session last took progress.
    /// </summary>
    public DateTime DateLastPlayed { get; set; }
}
