namespace SuCoS;

/// <summary>
/// Interface for the System.File class
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Directory.CreateDirectory
    /// </summary>
    /// <param name="path"></param>
    void DirectoryCreateDirectory(string path);

    /// <summary>
    /// Directory.Exists
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Directory.GetFiles
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string[] DirectoryGetFiles(string path);

    /// <summary>
    /// Directory.GetFiles
    /// </summary>
    /// <param name="path"></param>
    /// <param name="searchPattern"></param>
    /// <returns></returns>
    string[] DirectoryGetFiles(string path, string searchPattern);

    /// <summary>
    /// Directory.GetDirectories
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string[] DirectoryGetDirectories(string path);

    /// <summary>
    /// File.Exists
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    bool FileExists(string path);

    /// <summary>
    /// File.Copy
    /// </summary>
    /// <param name="sourceFileName"></param>
    /// <param name="destFileName"></param>
    /// <param name="overwrite"></param>
    /// <returns></returns>
    void FileCopy(string sourceFileName, string destFileName, bool overwrite);

    /// <summary>
    /// File.WriteAllText
    /// </summary>
    /// <param name="path"></param>
    /// <param name="contents"></param>
    /// <returns></returns>
    void FileWriteAllText(string path, string? contents);

    /// <summary>
    /// File.ReadAllText
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    string FileReadAllText(string path);
}

/// <summary>
/// Actual implementation of FS operations
/// </summary>
public class FileSystem : IFileSystem
{
    /// <inheritdoc/>
    public void DirectoryCreateDirectory(string path)
        => Directory.CreateDirectory(path);

    /// <inheritdoc/>
    public bool DirectoryExists(string path)
        => Directory.Exists(path);

    /// <inheritdoc/>
    public string[] DirectoryGetFiles(string path)
        => Directory.GetFiles(path);

    /// <inheritdoc/>
    public string[] DirectoryGetFiles(string path, string searchPattern)
        => Directory.GetFiles(path, searchPattern);

    /// <inheritdoc/>
    public string[] DirectoryGetDirectories(string path)
        => Directory.GetDirectories(path);

    /// <inheritdoc/>
    public bool FileExists(string path)
        => File.Exists(path);

    /// <inheritdoc/>
    public void FileCopy(string sourceFileName, string destFileName, bool overwrite)
        => File.Copy(sourceFileName, destFileName, overwrite);

    /// <inheritdoc/>
    public void FileWriteAllText(string path, string? contents)
        => File.WriteAllText(path, contents);

    /// <inheritdoc/>
    public string FileReadAllText(string path)
        => File.ReadAllText(path);
}
