using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Handle server requests
/// </summary>
internal interface IServerHandlers
{
    /// <summary>
    /// Check if the condition is met to handle the request
    /// </summary>
    /// <param name="requestPath"></param>
    /// <returns></returns>
    bool Check(string requestPath);

    /// <summary>
    /// Process the request
    /// </summary>
    /// <param name="context"></param>
    /// <param name="requestPath"></param>
    /// <param name="serverStartTime"></param>
    /// <returns></returns>
    Task<string> Handle(HttpContext context, string requestPath, DateTime serverStartTime);
}
