using SuCoS.Models;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Return the server startup timestamp as the response
/// </summary>
public class RegisteredPageResourceRequest : IServerHandlers
{
    private readonly ISite _site;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="site"></param>
    public RegisteredPageResourceRequest(ISite site)
    {
        _site = site;
    }

    /// <inheritdoc />
    public bool Check(string requestPath)
    {
        ArgumentNullException.ThrowIfNull(requestPath);

        return _site.OutputReferences.TryGetValue(requestPath, out var item) && item is IResource;
    }

    /// <inheritdoc />
    public async Task<string> Handle(IHttpListenerResponse response, string requestPath, DateTime serverStartTime)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!_site.OutputReferences.TryGetValue(requestPath, out var output) ||
            output is not IResource resource)
        {
            return "404";
        }
        response.ContentType = resource.MimeType;
        await using var fileStream = new FileStream(resource.SourceFullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await fileStream.CopyToAsync(response.OutputStream).ConfigureAwait(false);
        return "resource";

    }
}
