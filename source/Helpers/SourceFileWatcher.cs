using System;
using System.IO;

/// <summary>
/// The FileSystemWatcher object that monitors the source directory for file changes.
/// </summary>
public interface IFileWatcher
{
    /// <summary>
    /// Starts the file watcher to monitor file changes in the specified source path.
    /// </summary>
    /// <param name="SourceAbsolutePath">The path to the source directory.</param>
    /// <param name="OnSourceFileChanged"></param>
    /// <returns>The created FileSystemWatcher object.</returns>
    void Start(string SourceAbsolutePath, Action<object, FileSystemEventArgs> OnSourceFileChanged);

    /// <summary>
    /// Disposes the file watcher
    /// </summary>
    void Stop();
}

/// <summary>
/// The FileSystemWatcher object that monitors the source directory for file changes.
/// </summary>
public class SourceFileWatcher : IFileWatcher
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
        if (OnSourceFileChanged is null)
        {
            throw new ArgumentNullException(nameof(OnSourceFileChanged));
        }

        fileWatcher = new FileSystemWatcher
        {
            Path = SourceAbsolutePath,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
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
}
