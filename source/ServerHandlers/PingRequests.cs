using System;
using System.Threading.Tasks;
using System.IO;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Return the server startup timestamp as the response
/// </summary>
public class PingRequests : IServerHandlers
{
    /// <inheritdoc />
    public bool Check(string requestPath)
    {
        return requestPath == "/ping";
    }

    /// <inheritdoc />
    public async Task<string> Handle(IHttpListenerResponse response, string requestPath, DateTime serverStartTime)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }
        var content = serverStartTime.ToString("o");

        using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
        await writer.WriteAsync(content);
        await writer.FlushAsync();

        return "ping";
    }
}
