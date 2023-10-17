using System.IO;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Defines the contract for an HTTP listener response.
/// </summary>
public interface IHttpListenerResponse
{
    /// <summary>
    /// Gets the output stream for writing the response body.
    /// </summary>
    Stream OutputStream { get; }

    /// <summary>
    /// Gets or sets the content type of the response.
    /// </summary>
    string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the length of the content to be sent.
    /// </summary>
    long ContentLength64 { get; set; }
}
