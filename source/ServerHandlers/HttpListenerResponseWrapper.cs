using System.Net;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Provides a wrapper for the <see cref="HttpListenerResponse"/>.
/// </summary>
/// <remarks>
/// Wrap the HttpListenerResponse
/// </remarks>
/// <param name="response"></param>
public class HttpListenerResponseWrapper(HttpListenerResponse response) : IHttpListenerResponse
{
    private readonly HttpListenerResponse response = response;

    /// <inheritdoc />
    public Stream OutputStream => response.OutputStream;

    /// <inheritdoc />
    public string? ContentType
    {
        get => response.ContentType;
        set => response.ContentType = value;
    }

    /// <inheritdoc />
    public long ContentLength64
    {
        get => response.ContentLength64;
        set => response.ContentLength64 = value;
    }
}
