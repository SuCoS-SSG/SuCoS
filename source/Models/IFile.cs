using System.IO;
using FolkerKinzel.MimeTypes;

// using Microsoft.AspNetCore.StaticFiles;
using SuCoS.Helpers;

namespace SuCoS.Models;

/// <summary>
/// Basic structure needed to generate user content and system content
/// </summary>
public interface IFile
{
    /// <summary>
    /// The source filename, without the extension. ;)
    /// </summary>
    string SourceFullPath { get; }

    /// <summary>
    /// The source file/folder, relative to content folder
    /// </summary>
    string? SourceRelativePath { get; }

    /// <summary>
    /// The source directory of the file, without the file name.
    /// </summary>
    string? SourceRelativePathDirectory => Urlizer.Path(Path.GetDirectoryName(SourceFullPath));

    /// <summary>
    /// The full source directory of the file, without the file name.
    /// </summary>
    string? SourceFullPathDirectory => Urlizer.Path(Path.GetDirectoryName(SourceFullPath));

    /// <summary>
    /// The source filename, without the extension. ;)
    /// </summary>
    string? SourceFileNameWithoutExtension => Path.GetFileNameWithoutExtension(SourceFullPath);

    /// <summary>
    /// File extension.
    /// </summary>
    string Extension => Path.GetExtension(SourceFullPath);

    /// <summary>
    /// File MIME type.
    /// </summary>
    string MimeType => MimeString.FromFileName(SourceFullPath) ?? "application/octet-stream";

    /// <summary>
    /// File size in bytes.
    /// </summary>
    long Size => new FileInfo(SourceFullPath).Length;
}
