namespace SuCoS.Helpers;

/// <summary>
/// The FileSystemWatcher object that monitors the source directory for file changes.
/// </summary>
public interface IFileWatcher
{
    /// <summary>
    /// Starts the file watcher to monitor file changes in the specified source path.
    /// </summary>
    /// <param name="sourceAbsolutePath">The path to the source directory.</param>
    /// <param name="onSourceFileChanged"></param>
    /// <returns>The created FileSystemWatcher object.</returns>
    void Start(string sourceAbsolutePath, Action<object, FileSystemEventArgs> onSourceFileChanged);

    /// <summary>
    /// Disposes the file watcher
    /// </summary>
    void Stop();
}
