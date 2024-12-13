using FolkerKinzel.MimeTypes;
using SuCoS.Helpers;

namespace SuCoS.Models;

/// <summary>
/// Basic structure needed to generate user content and system content
/// </summary>
public interface IFile
{
    /// <summary>
    /// The source file/folder, relative to content folder
    /// </summary>
    string SourceRelativePath { get; }

    /// <summary>
    /// The source directory of the file, without the file name.
    /// </summary>
    string? SourceRelativePathDirectory => Urlizer.UnixPath(Path.GetDirectoryName(SourceRelativePath));

    /// <summary>
    /// The source filename, without the extension. ;)
    /// </summary>
    string? SourceFileNameWithoutExtension => Path.GetFileNameWithoutExtension(SourceRelativePath);

    /// <summary>
    /// File extension.
    /// </summary>
    string Extension => Path.GetExtension(SourceRelativePath);

    /// <summary>
    /// File MIME type.
    /// </summary>
    string MimeType => MimeString.FromFileName(SourceRelativePath);

    /// <summary>
    /// File size in bytes.
    /// </summary>
    long Size => new FileInfo(SourceRelativePath).Length;

    /// <summary>
    /// The source filename, without the extension. ;)
    /// </summary>
    string SourceFullPath(string basePath) =>
        Path.Combine(basePath, SourceRelativePath);

    /// <summary>
    /// The full source directory of the file, without the file name.
    /// </summary>
    string? SourceFullPathDirectory(string basePath) =>
        Urlizer.UnixPath(Path.GetDirectoryName(SourceFullPath(basePath)));
}
