using FolkerKinzel.MimeTypes;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Check if it is one of the Static files (serve the actual file)
/// </summary>
public class StaticFileRequest : IServerHandlers
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
        ArgumentNullException.ThrowIfNull(requestPath);

        var fileAbsolutePath = Path.Combine(basePath, requestPath.TrimStart('/'));
        return File.Exists(fileAbsolutePath);
    }

    /// <inheritdoc />
    public async Task<string> Handle(IHttpListenerResponse response, string requestPath, DateTime serverStartTime)
    {
        ArgumentNullException.ThrowIfNull(requestPath);
        ArgumentNullException.ThrowIfNull(response);

        var fileAbsolutePath = Path.Combine(basePath, requestPath.TrimStart('/'));
        response.ContentType = GetContentType(fileAbsolutePath!);
        await using var fileStream = new FileStream(fileAbsolutePath!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        response.ContentLength64 = fileStream.Length;
        await fileStream.CopyToAsync(response.OutputStream).ConfigureAwait(false);
        return inTheme ? "themeSt" : "static";
    }

    /// <summary>
    /// Retrieves the content type of a file based on its extension.
    /// If the content type cannot be determined, the default value "application/octet-stream" is returned.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <returns>The content type of the file.</returns>
    private static string GetContentType(string filePath) =>
        MimeString.FromFileName(filePath) ?? "application/octet-stream";
}
