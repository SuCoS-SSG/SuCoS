using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using SuCoS.Models;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Return the server startup timestamp as the response
/// </summary>
public class RegisteredPageRequest : IServerHandlers
{
    readonly ISite site;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="site"></param>
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
        return site.OutputReferences.TryGetValue(requestPath, out var item) && item is IPage _;
    }

    /// <inheritdoc />
    public async Task<string> Handle(IHttpListenerResponse response, string requestPath, DateTime serverStartTime)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }
        if (site.OutputReferences.TryGetValue(requestPath, out var output) && output is IPage page)
        {
            var content = page.CompleteContent;
            content = InjectReloadScript(content);
            using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
            await writer.WriteAsync(content);
            return "dict";
        }
        else
        {
            return "404";
        }
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
