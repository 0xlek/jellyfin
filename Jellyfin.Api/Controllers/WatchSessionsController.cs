using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Each device's progress through items, for the signed-in user.
/// </summary>
[Authorize]
[Tags("WatchSessions")]
public class WatchSessionsController : BaseJellyfinApiController
{
    private readonly IWatchSessionManager _watchSessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchSessionsController"/> class.
    /// </summary>
    /// <param name="watchSessionManager">The watch session manager.</param>
    public WatchSessionsController(IWatchSessionManager watchSessionManager)
    {
        _watchSessionManager = watchSessionManager;
    }

    /// <summary>
    /// Gets the user's watch sessions, most recently played first.
    /// </summary>
    /// <param name="itemId">Only the sessions of this item.</param>
    /// <param name="limit">The maximum number of sessions.</param>
    /// <response code="200">The sessions.</response>
    /// <returns>The sessions.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WatchSessionDto>>> GetWatchSessions([FromQuery] Guid? itemId, [FromQuery, Range(1, 1000)] int? limit)
    {
        return Ok(await _watchSessionManager.GetSessionsAsync(User.GetUserId(), itemId, limit).ConfigureAwait(false));
    }

    /// <summary>
    /// Moves a session to any position. Playback only ever moves a session forward; this is the way back.
    /// </summary>
    /// <param name="sessionId">The session.</param>
    /// <param name="positionTicks">The new position.</param>
    /// <response code="200">The updated session.</response>
    /// <response code="404">The user has no such session.</response>
    /// <returns>The updated session.</returns>
    [HttpPost("{sessionId}/Position")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchSessionDto>> SetWatchSessionPosition([FromRoute, Required] Guid sessionId, [FromQuery, Required, Range(0, long.MaxValue)] long positionTicks)
    {
        var session = await _watchSessionManager.SetPositionAsync(User.GetUserId(), sessionId, positionTicks).ConfigureAwait(false);
        return session is null ? NotFound() : Ok(session);
    }

    /// <summary>
    /// Deletes a session.
    /// </summary>
    /// <param name="sessionId">The session.</param>
    /// <response code="204">Deleted.</response>
    /// <response code="404">The user has no such session.</response>
    /// <returns>No content.</returns>
    [HttpDelete("{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteWatchSession([FromRoute, Required] Guid sessionId)
    {
        return await _watchSessionManager.DeleteAsync(User.GetUserId(), sessionId).ConfigureAwait(false) ? NoContent() : NotFound();
    }
}
