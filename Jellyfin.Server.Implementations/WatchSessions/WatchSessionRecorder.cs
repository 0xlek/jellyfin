using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.WatchSessions;

/// <summary>
/// Feeds every playback report from a client into that device's watch session.
/// </summary>
public class WatchSessionRecorder : IEventConsumer<PlaybackProgressEventArgs>, IEventConsumer<PlaybackStopEventArgs>
{
    private readonly IWatchSessionManager _watchSessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchSessionRecorder"/> class.
    /// </summary>
    /// <param name="watchSessionManager">The watch session manager.</param>
    public WatchSessionRecorder(IWatchSessionManager watchSessionManager)
    {
        _watchSessionManager = watchSessionManager;
    }

    /// <inheritdoc />
    public Task OnEvent(PlaybackProgressEventArgs eventArgs)
    {
        // Automated reports are the server's own guesses between check-ins, not positions the device reached
        return eventArgs.IsAutomated ? Task.CompletedTask : Record(eventArgs, false);
    }

    /// <inheritdoc />
    public Task OnEvent(PlaybackStopEventArgs eventArgs) => Record(eventArgs, eventArgs.PlayedToCompletion);

    private async Task Record(PlaybackProgressEventArgs eventArgs, bool playedToCompletion)
    {
        if (eventArgs.Item is not Video video || video.IsThemeMedia || eventArgs.PlaybackPositionTicks is not long position || string.IsNullOrEmpty(eventArgs.DeviceId))
        {
            return;
        }

        foreach (var user in eventArgs.Users)
        {
            await _watchSessionManager.RecordAsync(user.Id, video, eventArgs.DeviceId, eventArgs.DeviceName, eventArgs.ClientName, eventArgs.PlaySessionId, position, playedToCompletion).ConfigureAwait(false);
        }
    }
}
