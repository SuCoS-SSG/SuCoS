using FolkerKinzel.MimeTypes;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Check if it is one of the Static files (serve the actual file)
/// </summary>
public class StaticFileRequest(string? basePath, bool inTheme) : IServerHandlers
{
    /// <inheritdoc />
    public bool Check(string requestPath)
    {
        ArgumentNullException.ThrowIfNull(requestPath);

        if (string.IsNullOrEmpty(basePath))
        {
            return false;
        }

        var fileAbsolutePath = Path.Combine(basePath, requestPath.TrimStart('/'));
        return File.Exists(fileAbsolutePath);
    }

    /// <inheritdoc />
    public async Task<string> Handle(IHttpListenerResponse response, string requestPath, DateTime serverStartTime)
    {
        ArgumentNullException.ThrowIfNull(requestPath);
        ArgumentNullException.ThrowIfNull(response);

        var fileAbsolutePath = Path.Combine(basePath!, requestPath.TrimStart('/'));
        response.ContentType = GetContentType(fileAbsolutePath);
        await using var fileStream = new FileStream(fileAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        response.ContentLength64 = fileStream.Length;
        await fileStream.CopyToAsync(response.OutputStream).ConfigureAwait(false);
        return inTheme ? "themeSt" : "static";
    }

    /// <summary>
    /// Retrieves the content type of file based on its extension.
    /// If the content type cannot be determined, the default value "application/octet-stream" is returned.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <returns>The content type of the file.</returns>
    private static string GetContentType(string filePath) => MimeString.FromFileName(filePath);
}
