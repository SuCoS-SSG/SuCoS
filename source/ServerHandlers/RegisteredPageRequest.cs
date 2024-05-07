using SuCoS.Models;
using System.Reflection;

namespace SuCoS.ServerHandlers;

/// <summary>
/// Return the server startup timestamp as the response
/// </summary>
public class RegisteredPageRequest : IServerHandlers
{
    private readonly ISite _site;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="site"></param>
    public RegisteredPageRequest(ISite site)
    {
        _site = site;
    }

    /// <inheritdoc />
    public bool Check(string requestPath)
    {
        ArgumentNullException.ThrowIfNull(requestPath);

        return _site.OutputReferences.TryGetValue(requestPath, out var item) && item is IPage;
    }

    /// <inheritdoc />
    public async Task<string> Handle(IHttpListenerResponse response, string requestPath, DateTime serverStartTime)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!_site.OutputReferences.TryGetValue(requestPath, out var output) ||
            output is not IPage page)
        {
            return "404";
        }
        var content = page.CompleteContent;
        content = InjectReloadScript(content);
        await using var writer = new StreamWriter(response.OutputStream, leaveOpen: true);
        await writer.WriteAsync(content).ConfigureAwait(false);
        return "dict";

    }

    /// <summary>
    /// Injects a reload script into the provided content.
    /// The script is read from a JavaScript file and injected before the closing "body" tag.
    /// </summary>
    /// <param name="content">The content to inject the reload script into.</param>
    /// <returns>The content with the reload script injected.</returns>
    private static string InjectReloadScript(string content)
    {
        // Read the content of the JavaScript file
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("SuCoS.wwwroot.js.reload.js")
            ?? throw new FileNotFoundException("Could not find the embedded JavaScript resource.");
        using var reader = new StreamReader(stream);
        var scriptContent = reader.ReadToEnd();

        // Inject the JavaScript content
        var reloadScript = $"<script>{scriptContent}</script>";

        const string bodyClosingTag = "</body>";
        content = content.Replace(bodyClosingTag, $"{reloadScript}{bodyClosingTag}", StringComparison.InvariantCulture);

        return content;
    }
}
