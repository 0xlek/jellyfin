#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.IO
{
    public sealed class FileRefresher : IDisposable
    {
        // Refreshers for different folders finish independently; their library writes go one at a time
        private static readonly SemaphoreSlim _refreshLock = new(1, 1);

        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _configurationManager;

        private readonly List<string> _affectedPaths = new();
        private readonly Lock _timerLock = new();
        private Timer? _timer;
        private bool _disposed;

        public FileRefresher(string path, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ILogger logger)
        {
            logger.LogDebug("New file refresher created for {0}", path);
            Path = path;

            _configurationManager = configurationManager;
            _libraryManager = libraryManager;
            _logger = logger;
            AddPath(path);
        }

        public event EventHandler<EventArgs>? Completed;

        public string Path { get; private set; }

        private void AddAffectedPath(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            if (!_affectedPaths.Contains(path, StringComparer.Ordinal))
            {
                _affectedPaths.Add(path);
            }
        }

        public void AddPath(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            lock (_timerLock)
            {
                AddAffectedPath(path);
            }

            RestartTimer();
        }

        public void RestartTimer()
        {
            if (_disposed)
            {
                return;
            }

            lock (_timerLock)
            {
                if (_disposed)
                {
                    return;
                }

                if (_timer is null)
                {
                    _timer = new Timer(OnTimerCallback, null, TimeSpan.FromSeconds(_configurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
                else
                {
                    _timer.Change(TimeSpan.FromSeconds(_configurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        public void ResetPath(string path, string? affectedFile)
        {
            lock (_timerLock)
            {
                _logger.LogDebug("Resetting file refresher from {0} to {1}", Path, path);

                Path = path;
                AddAffectedPath(path);

                if (!string.IsNullOrEmpty(affectedFile))
                {
                    AddAffectedPath(affectedFile);
                }
            }

            RestartTimer();
        }

        private void OnTimerCallback(object? state)
        {
            List<string> paths;

            lock (_timerLock)
            {
                if (_disposed)
                {
                    return;
                }

                paths = _affectedPaths.ToList();
            }

            _logger.LogDebug("Timer stopped.");

            DisposeTimer();
            Completed?.Invoke(this, EventArgs.Empty);

            _ = ProcessPathChangesAsync(paths);
        }

        /// <summary>
        /// Applies changes file by file: only the folder holding a change is re-read, without descending into the rest
        /// of the tree, a newly added folder is read in full, and only the changed paths are probed. Refreshing the
        /// whole parent folder instead made one new file wait on a rescan of everything beside it.
        /// </summary>
        private async Task ProcessPathChangesAsync(List<string> paths)
        {
            var plans = new Dictionary<Guid, RefreshPlan>();
            var fallback = new Dictionary<Guid, BaseItem>();

            foreach (var path in paths.Distinct(StringComparer.Ordinal))
            {
                var item = TryGetAffectedBaseItem(path);
                if (item is null || item is AggregateFolder)
                {
                    continue;
                }

                try
                {
                    PlanChange(path, item, plans, fallback);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error planning the refresh for {Path}", path);
                }
            }

            await _refreshLock.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var plan in plans.Values)
                {
                    await RunPlan(plan).ConfigureAwait(false);
                }

                foreach (var item in fallback.Values)
                {
                    _logger.LogInformation("{Name} ({Path}) will be refreshed.", item.Name, item.Path);
                    try
                    {
                        item.ChangedExternally();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing {Name}", item.Name);
                    }
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private static void PlanChange(string path, BaseItem item, Dictionary<Guid, RefreshPlan> plans, Dictionary<Guid, BaseItem> fallback)
        {
            // The changed path is the item itself: re-read the folder it sits in, probing just this path
            var isSelf = string.Equals(item.Path, path, StringComparison.Ordinal);
            var folder = isSelf ? item.GetParent() as Folder : item as Folder ?? item.GetParent() as Folder;

            // Library roots and containers such as series keep the stock whole-item refresh
            if (folder is null or CollectionFolder or AggregateFolder || folder is IMetadataContainer || item is IMetadataContainer)
            {
                fallback[item.Id] = item;
                return;
            }

            if (!plans.TryGetValue(folder.Id, out var plan))
            {
                plan = new RefreshPlan(folder);
                plans[folder.Id] = plan;
            }

            if (isSelf)
            {
                plan.Paths.Add(path);
                return;
            }

            // A new or removed path below the folder: the entry directly inside it is what appears or goes
            var entry = path;
            while (System.IO.Path.GetDirectoryName(entry) is { } parent && !string.Equals(parent, folder.Path, StringComparison.Ordinal))
            {
                entry = parent;
            }

            plan.Paths.Add(entry);
            if (Directory.Exists(entry))
            {
                plan.NewFolders.Add(entry);
                plan.Paths.UnionWith(Directory.EnumerateFileSystemEntries(entry, "*", SearchOption.AllDirectories));
            }
        }

        private async Task RunPlan(RefreshPlan plan)
        {
            var options = new MetadataRefreshOptions(new DirectoryService(BaseItem.FileSystem))
            {
                RefreshPaths = plan.Paths.ToArray()
            };

            _logger.LogInformation("{Name} ({Path}) will be refreshed for {Count} changed path(s).", plan.Folder.Name, plan.Folder.Path, plan.Paths.Count);

            try
            {
                await plan.Folder.ValidateChildren(new Progress<double>(), options, recursive: false).ConfigureAwait(false);

                foreach (var path in plan.NewFolders)
                {
                    if (_libraryManager.FindByPath(path, true) is Folder added)
                    {
                        await added.ValidateChildren(new Progress<double>(), options, recursive: true).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing {Name}", plan.Folder.Name);
            }
        }

        private BaseItem? TryGetAffectedBaseItem(string path)
        {
            try
            {
                return GetAffectedBaseItem(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding the item affected by changes to {Path}", path);
                return null;
            }
        }

        /// <summary>
        /// Gets the affected base item.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem? GetAffectedBaseItem(string path)
        {
            BaseItem? item = null;

            while (item is null && !string.IsNullOrEmpty(path))
            {
                item = _libraryManager.FindByPath(path, null);

                path = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
            }

            if (item is not null)
            {
                // If the item has been deleted find the first valid parent that still exists
                while (!Directory.Exists(item.Path) && !File.Exists(item.Path))
                {
                    item = item.GetOwner() ?? item.GetParent();

                    if (item is null)
                    {
                        break;
                    }
                }
            }

            return item;
        }

        private sealed class RefreshPlan(Folder folder)
        {
            public Folder Folder { get; } = folder;

            public HashSet<string> Paths { get; } = new(StringComparer.Ordinal);

            public HashSet<string> NewFolders { get; } = new(StringComparer.Ordinal);
        }

        private void DisposeTimer()
        {
            lock (_timerLock)
            {
                if (_timer is not null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            DisposeTimer();
            _disposed = true;
        }
    }
}
