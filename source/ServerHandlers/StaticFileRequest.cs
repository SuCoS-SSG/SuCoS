using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Check if it is one of the Static files (serve the actual file)
/// </summary>
internal class StaticFileRequest : IServerHandlers
{
    private readonly string basePath;
    private readonly bool inTheme;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="basePath"></param>
    /// <param name="inTheme"></param>
    public StaticFileRequest(string basePath, bool inTheme)
    {
        this.basePath = basePath;
        this.inTheme = inTheme;
    }

    /// <inheritdoc />
    public bool Check(string requestPath)
    {
        if (requestPath is null)
        {
            throw new ArgumentNullException(nameof(requestPath));
        }
        var fileAbsolutePath = Path.Combine(basePath, requestPath.TrimStart('/'));
        return File.Exists(fileAbsolutePath);
    }

    /// <inheritdoc />
    public async Task<string> Handle(HttpContext context, string requestPath, DateTime serverStartTime)
    {
        var fileAbsolutePath = Path.Combine(basePath, requestPath.TrimStart('/'));
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        context.Response.ContentType = GetContentType(fileAbsolutePath!);
        await using var fileStream = new FileStream(fileAbsolutePath!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await fileStream.CopyToAsync(context.Response.Body);
        return inTheme ? "themeSt" : "static";
    }

    /// <summary>
    /// Retrieves the content type of a file based on its extension.
    /// If the content type cannot be determined, the default value "application/octet-stream" is returned.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <returns>The content type of the file.</returns>
    private static string GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType ?? "application/octet-stream";
    }
}
