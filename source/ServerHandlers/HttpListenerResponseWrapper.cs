using System.Net;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Provides a wrapper for the <see cref="HttpListenerResponse"/>.
/// </summary>
public class HttpListenerResponseWrapper : IHttpListenerResponse
{
    private readonly HttpListenerResponse response;

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

    /// <summary>
    /// Wrap the HttpListenerResponse
    /// </summary>
    /// <param name="response"></param>
    public HttpListenerResponseWrapper(HttpListenerResponse response)
    {
        this.response = response;
    }
}
