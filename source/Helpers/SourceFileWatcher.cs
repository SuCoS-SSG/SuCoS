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
    private FileSystemWatcher? _fileWatcher;

    /// <inheritdoc/>
    public void Start(string sourceAbsolutePath, Action<object, FileSystemEventArgs> onSourceFileChanged)
    {
        ArgumentNullException.ThrowIfNull(onSourceFileChanged);

        _fileWatcher = new FileSystemWatcher
        {
            Path = sourceAbsolutePath,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        // Subscribe to the desired events
        _fileWatcher.Changed += new FileSystemEventHandler(onSourceFileChanged.Invoke);
        _fileWatcher.Created += new FileSystemEventHandler(onSourceFileChanged.Invoke);
        _fileWatcher.Deleted += new FileSystemEventHandler(onSourceFileChanged.Invoke);
        _fileWatcher.Renamed += new RenamedEventHandler(onSourceFileChanged);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _fileWatcher?.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fileWatcher?.Dispose();
        }
    }
}
