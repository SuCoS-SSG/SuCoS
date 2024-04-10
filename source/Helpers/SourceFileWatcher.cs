using System;

namespace SuCoS.Helpers;

/// <summary>
/// The FileSystemWatcher object that monitors the source directory for file changes.
/// </summary>
public sealed class SourceFileWatcher : IFileWatcher, IDisposable
{
    /// <summary>
    /// The FileSystemWatcher object that monitors the source directory for file changes.
    /// When a change is detected, this triggers a server restart to ensure the served content
    /// remains up-to-date. The FileSystemWatcher is configured with the source directory
    /// at construction and starts watching immediately.
    /// </summary>
    private FileSystemWatcher? fileWatcher;

    /// <inheritdoc/>
    public void Start(string SourceAbsolutePath, Action<object, FileSystemEventArgs> OnSourceFileChanged)
    {
        ArgumentNullException.ThrowIfNull(OnSourceFileChanged);

        fileWatcher = new FileSystemWatcher
        {
            Path = SourceAbsolutePath,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        // Subscribe to the desired events
        fileWatcher.Changed += new FileSystemEventHandler(OnSourceFileChanged.Invoke);
        fileWatcher.Created += new FileSystemEventHandler(OnSourceFileChanged.Invoke);
        fileWatcher.Deleted += new FileSystemEventHandler(OnSourceFileChanged.Invoke);
        fileWatcher.Renamed += new RenamedEventHandler(OnSourceFileChanged);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        fileWatcher?.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (disposing)
        {
            fileWatcher?.Dispose();
        }
    }
}
