using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SuCoS.Models;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Return the server startup timestamp as the response
/// </summary>
internal class RegisteredPageRequest : IServerHandlers
{
    readonly ISite site;

    public RegisteredPageRequest(ISite site)
    {
        this.site = site;
    }

    /// <inheritdoc />
    public bool Check(string requestPath)
    {
        if (requestPath is null)
        {
            throw new ArgumentNullException(nameof(requestPath));
        }
        return site.PagesReferences.TryGetValue(requestPath, out _);
    }

    /// <inheritdoc />
    public async Task<string> Handle(HttpContext context, string requestPath, DateTime serverStartTime)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        site.PagesReferences.TryGetValue(requestPath, out var page);
        var content = page!.CompleteContent;
        content = InjectReloadScript(content);
        await context.Response.WriteAsync(content);
        return "dict";
    }

    /// <summary>
    /// Injects a reload script into the provided content.
    /// The script is read from a JavaScript file and injected before the closing "body" tag.
    /// </summary>
    /// <param name="content">The content to inject the reload script into.</param>
    /// <returns>The content with the reload script injected.</returns>
    private string InjectReloadScript(string content)
    {
        // Read the content of the JavaScript file
        string scriptContent;
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("SuCoS.wwwroot.js.reload.js")
            ?? throw new FileNotFoundException("Could not find the embedded JavaScript resource.");
        using var reader = new StreamReader(stream);
        scriptContent = reader.ReadToEnd();

        // Inject the JavaScript content
        var reloadScript = $"<script>{scriptContent}</script>";

        const string bodyClosingTag = "</body>";
        content = content.Replace(bodyClosingTag, $"{reloadScript}{bodyClosingTag}", StringComparison.InvariantCulture);

        return content;
    }
}
