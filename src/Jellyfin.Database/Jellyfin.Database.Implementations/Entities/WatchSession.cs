using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// One device's progress through an item for a user. The position only moves forward while playing;
/// only an explicit edit can move it back.
/// </summary>
public class WatchSession
{
    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the item id.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the id of the device that played it.
    /// </summary>
    [MaxLength(256)]
    public required string DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device name last reported.
    /// </summary>
    [MaxLength(256)]
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the client name last reported.
    /// </summary>
    [MaxLength(256)]
    public string? Client { get; set; }

    /// <summary>
    /// Gets or sets the playback that last reported to this session; its later reports keep landing here even once it is finished.
    /// </summary>
    [MaxLength(256)]
    public string? PlaySessionId { get; set; }

    /// <summary>
    /// Gets or sets the furthest position reached, in ticks.
    /// </summary>
    public long PositionTicks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item was watched to the end; a finished session takes no more progress.
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

    /// <summary>
    /// Gets or sets the user.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Gets or sets the item.
    /// </summary>
    public BaseItemEntity? Item { get; set; }
}
