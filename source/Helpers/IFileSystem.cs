namespace SuCoS;

/// <summary>
/// Interface for the System.File class
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Path.GetFullPath
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string GetFullPath(string path);

    /// <summary>
    /// Path.Combine
    /// </summary>
    /// <param name="path1"></param>
    /// <param name="path2"></param>
    /// <returns></returns>
    string Combine(string path1, string path2);

    /// <summary>
    /// File.Exists
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    bool FileExists(string path);

    /// <summary>
    /// Directory.CreateDirectory
    /// </summary>
    /// <param name="path"></param>
    void CreateDirectory(string path);
}

/// <summary>
/// Actual implementation of FS operations
/// </summary>
public class FileSystem : IFileSystem
{
    /// <inheritdoc/>
    public string GetFullPath(string path) => Path.GetFullPath(path);

    /// <inheritdoc/>
    public string Combine(string path1, string path2) => Path.Combine(path1, path2);

    /// <inheritdoc/>
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc/>
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
}
