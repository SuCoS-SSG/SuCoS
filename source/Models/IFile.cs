using System.IO;
using Microsoft.AspNetCore.StaticFiles;

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
    string? SourceRelativePathDirectory => Path.GetDirectoryName(SourceRelativePath);

    /// <summary>
    /// The full source directory of the file, without the file name.
    /// </summary>
    string? SourceFullPathDirectory => Path.GetDirectoryName(SourceFullPath);

    /// <summary>
    /// The source filename, without the extension. ;)
    /// </summary>
    string? SourceFileNameWithoutExtension => Path.GetFileNameWithoutExtension(SourceRelativePath);

    /// <summary>
    /// File extension.
    /// </summary>
    string Extension => Path.GetExtension(SourceFullPath);

    /// <summary>
    /// File MIME type.
    /// </summary>
    string MimeType
    {
        get
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(SourceFullPath, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }
    }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    long Size => new FileInfo(SourceFullPath).Length;
}
