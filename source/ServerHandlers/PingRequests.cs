using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Return the server startup timestamp as the response
/// </summary>
internal class PingRequests : IServerHandlers
{
    /// <inheritdoc />
    public bool Check(string requestPath)
    {
        return requestPath == "/ping";
    }

    /// <inheritdoc />
    public async Task<string> Handle(HttpContext context, string requestPath, DateTime serverStartTime)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        var content = serverStartTime.ToString("o");
        await context.Response.WriteAsync(content);

        return "ping";
    }
}
