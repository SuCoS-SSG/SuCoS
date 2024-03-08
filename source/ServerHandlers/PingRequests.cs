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
        ArgumentNullException.ThrowIfNull(response);

        var content = serverStartTime.ToString("o");

        using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
        await writer.WriteAsync(content).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);

        return "ping";
    }
}
