using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.WatchSessions;

/// <inheritdoc />
public sealed class WatchSessionManager : IWatchSessionManager, IDisposable
{
    /// <summary>
    /// A device that only opened an item, without watching any of it, gets no session.
    /// </summary>
    private static readonly long _minStartTicks = TimeSpan.FromSeconds(10).Ticks;

    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerConfigurationManager _config;

    // Two reports from one device can arrive together; both must not open a session
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchSessionManager"/> class.
    /// </summary>
    /// <param name="dbProvider">The database factory.</param>
    /// <param name="config">The server configuration.</param>
    public WatchSessionManager(IDbContextFactory<JellyfinDbContext> dbProvider, IServerConfigurationManager config)
    {
        _dbProvider = dbProvider;
        _config = config;
    }

    /// <inheritdoc />
    public async Task RecordAsync(Guid userId, BaseItem item, string deviceId, string? deviceName, string? client, string? playSessionId, long positionTicks, bool playedToCompletion)
    {
        var finished = IsFinished(item.RunTimeTicks, positionTicks, playedToCompletion);
        var now = DateTime.UtcNow;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (db.ConfigureAwait(false))
            {
                var session = await db.WatchSessions
                    .Where(s => s.UserId.Equals(userId) && s.ItemId.Equals(item.Id) && s.DeviceId == deviceId
                        && (!s.IsFinished || (playSessionId != null && s.PlaySessionId == playSessionId)))
                    .OrderByDescending(s => s.DateLastPlayed)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);

                if (session is null)
                {
                    if (positionTicks < _minStartTicks)
                    {
                        return;
                    }

                    session = new WatchSession
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ItemId = item.Id,
                        DeviceId = deviceId,
                        DateCreated = now
                    };
                    db.WatchSessions.Add(session);
                }
                else if (positionTicks <= session.PositionTicks)
                {
                    return;
                }

                session.PositionTicks = positionTicks;
                session.IsFinished |= finished;
                session.PlaySessionId = playSessionId;
                session.DeviceName = deviceName;
                session.Client = client;
                session.DateLastPlayed = now;
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WatchSessionDto>> GetSessionsAsync(Guid userId, Guid? itemId, int? limit)
    {
        var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (db.ConfigureAwait(false))
        {
            var query = db.WatchSessions.AsNoTracking().Where(s => s.UserId.Equals(userId));
            if (itemId.HasValue)
            {
                query = query.Where(s => s.ItemId.Equals(itemId.Value));
            }

            query = query.OrderByDescending(s => s.DateLastPlayed);
            if (limit.HasValue)
            {
                query = query.Take(limit.Value);
            }

            var sessions = await query.ToListAsync().ConfigureAwait(false);
            return sessions.Select(ToDto).ToArray();
        }
    }

    /// <inheritdoc />
    public async Task<WatchSessionDto?> SetPositionAsync(Guid userId, Guid sessionId, long positionTicks)
    {
        var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (db.ConfigureAwait(false))
        {
            var session = await db.WatchSessions
                .Include(s => s.Item)
                .FirstOrDefaultAsync(s => s.Id.Equals(sessionId) && s.UserId.Equals(userId))
                .ConfigureAwait(false);
            if (session is null)
            {
                return null;
            }

            session.PositionTicks = Math.Max(positionTicks, 0);
            session.IsFinished = IsFinished(session.Item?.RunTimeTicks, session.PositionTicks, false);
            await db.SaveChangesAsync().ConfigureAwait(false);
            return ToDto(session);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid userId, Guid sessionId)
    {
        var db = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (db.ConfigureAwait(false))
        {
            var deleted = await db.WatchSessions
                .Where(s => s.Id.Equals(sessionId) && s.UserId.Equals(userId))
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);
            return deleted > 0;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _lock.Dispose();

    private static WatchSessionDto ToDto(WatchSession session) => new()
    {
        Id = session.Id,
        ItemId = session.ItemId,
        DeviceId = session.DeviceId,
        DeviceName = session.DeviceName,
        Client = session.Client,
        PositionTicks = session.PositionTicks,
        IsFinished = session.IsFinished,
        DateCreated = session.DateCreated,
        DateLastPlayed = session.DateLastPlayed
    };

    /// <summary>
    /// Finished at the same point the server stops offering to resume; without a runtime, only the server's own verdict counts.
    /// </summary>
    private bool IsFinished(long? runTimeTicks, long positionTicks, bool playedToCompletion)
    {
        if (runTimeTicks is not > 0)
        {
            return playedToCompletion;
        }

        return positionTicks >= runTimeTicks.Value - TimeSpan.TicksPerSecond
            || positionTicks * 100.0 / runTimeTicks.Value > _config.Configuration.MaxResumePct;
    }
}
