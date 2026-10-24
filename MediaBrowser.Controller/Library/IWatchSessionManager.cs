using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Keeps each device's progress through an item apart, so devices never overwrite each other.
/// </summary>
public interface IWatchSessionManager
{
    /// <summary>
    /// Records a playback position from a device. The device's open session only moves forward; a device without one,
    /// or whose last one is finished, starts a new session on its next playback.
    /// </summary>
    /// <param name="userId">The user.</param>
    /// <param name="item">The item played.</param>
    /// <param name="deviceId">The device id.</param>
    /// <param name="deviceName">The device name.</param>
    /// <param name="client">The client name.</param>
    /// <param name="playSessionId">The playback the report belongs to.</param>
    /// <param name="positionTicks">The position reached.</param>
    /// <param name="playedToCompletion">Whether the server considers the item played to the end.</param>
    /// <returns>A task.</returns>
    Task RecordAsync(Guid userId, BaseItem item, string deviceId, string? deviceName, string? client, string? playSessionId, long positionTicks, bool playedToCompletion);

    /// <summary>
    /// Gets a user's sessions, most recently played first.
    /// </summary>
    /// <param name="userId">The user.</param>
    /// <param name="itemId">Only the sessions of this item.</param>
    /// <param name="limit">The maximum number of sessions.</param>
    /// <returns>The sessions.</returns>
    Task<IReadOnlyList<WatchSessionDto>> GetSessionsAsync(Guid userId, Guid? itemId, int? limit);

    /// <summary>
    /// Moves a session to any position, backwards included.
    /// </summary>
    /// <param name="userId">The user that owns the session.</param>
    /// <param name="sessionId">The session.</param>
    /// <param name="positionTicks">The new position.</param>
    /// <returns>The updated session, or null when the user has no such session.</returns>
    Task<WatchSessionDto?> SetPositionAsync(Guid userId, Guid sessionId, long positionTicks);

    /// <summary>
    /// Deletes a session.
    /// </summary>
    /// <param name="userId">The user that owns the session.</param>
    /// <param name="sessionId">The session.</param>
    /// <returns>Whether a session was deleted.</returns>
    Task<bool> DeleteAsync(Guid userId, Guid sessionId);
}
