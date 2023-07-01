using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SuCoS.Helper;
using SuCoS.Models;

namespace SuCoS;

/// <summary>
/// Serve Command will live serve the site and watch any changes.
/// </summary>
public class ServeCommand : BaseGeneratorCommand, IDisposable
{
    /// <summary>
    /// The ServeOptions object containing the configuration parameters for the server.
    /// This includes settings such as the source directory to watch for file changes, 
    /// verbosity of the logs, etc. These options are passed into the ServeCommand at construction 
    /// and are used throughout its operation.
    /// </summary>
    private readonly ServeOptions options;

    /// <summary>
    /// The global base URL for the server, combined with the port number.
    /// This is constructed in the ServeCommand constructor using the provided base URL and port number,
    /// and is subsequently used when starting or restarting the server.
    /// </summary>
    private readonly string baseURLGlobal;

    /// <summary>
    /// An instance of IWebHost, which represents the running web server.
    /// This instance is created every time the server starts or restarts and is used to manage 
    /// the lifecycle of the server. It's nullable because there may be periods when there is no running server,
    /// such as during a restart operation or before the server has been initially started.
    /// </summary>
    private IWebHost? host;

    /// <summary>
    /// The FileSystemWatcher object that monitors the source directory for file changes.
    /// When a change is detected, this triggers a server restart to ensure the served content 
    /// remains up-to-date. The FileSystemWatcher is configured with the source directory 
    /// at construction and starts watching immediately.
    /// </summary>
    private readonly FileSystemWatcher sourceFileWatcher;

    /// <summary>
    /// A Timer that helps to manage the frequency of server restarts.
    /// When a file change is detected, the timer is reset. The action (server restart) only occurs
    /// when the timer expires, helping to ensure that rapid consecutive file changes result
    /// in a single server restart, not multiple.
    /// </summary>
    private Timer? debounceTimer;

    /// <summary>
    /// A boolean flag indicating whether a server restart is currently in progress. 
    /// This is used to prevent overlapping restarts when file changes are detected in quick succession.
    /// </summary>
    private volatile bool restartInProgress = false;

    /// <summary>
    /// A SemaphoreSlim used to ensure that server restarts due to file changes occur sequentially, 
    /// not concurrently. This is necessary because a restart involves stopping the current server
    /// and starting a new one, which would not be thread-safe without some form of synchronization.
    /// </summary>
    private readonly SemaphoreSlim restartServerLock = new(1, 1);

    private DateTime serverStartTime;

    /// <summary>
    /// Method to start the server explicitly
    /// </summary>
    /// <returns></returns>
    public async Task RunServer()
    {
        // Start the server!
        await StartServer("http://localhost", 1122);
    }

    /// <summary>
    /// Starts the server asynchronously.
    /// </summary>
    /// <param name="baseURL">The base URL for the server.</param>
    /// <param name="port">The port number for the server.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task StartServer(string baseURL, int port)
    {
        logger.Information("Starting server...");

        // Generate the build report
        stopwatch.LogReport(site.Title);

        serverStartTime = DateTime.UtcNow;

        host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(baseURLGlobal)
            .UseContentRoot(options.Source) // Set the content root path
            .UseEnvironment("Debug") // Set the hosting environment to Debug
            .Configure(app =>
            {
                app.Run(HandleRequest); // Call the custom method for handling requests
            })
            .Build();

        await host.StartAsync();
        logger.Information("You site is live: {baseURL}:{port}", baseURL, port);

    }

    /// <inheritdoc/>
    public void Dispose()
    {
        host?.Dispose();
        sourceFileWatcher?.Dispose();
        debounceTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Constructor for the ServeCommand class.
    /// </summary>
    /// <param name="options">ServeOptions object specifying the serve options.</param>
    /// <param name="logger">The logger instance. Injectable for testing</param>
    public ServeCommand(ServeOptions options, ILogger logger) : base(options, logger)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        var baseURL = "http://localhost";
        var port = 1122;
        baseURLGlobal = $"{baseURL}:{port}";

        // Watch for file changes in the specified path
        sourceFileWatcher = StartFileWatcher(options.Source);
    }

    /// <summary>
    /// Starts the file watcher to monitor file changes in the specified source path.
    /// </summary>
    /// <param name="SourcePath">The path to the source directory.</param>
    /// <returns>The created FileSystemWatcher object.</returns>
    private FileSystemWatcher StartFileWatcher(string SourcePath)
    {
        var SourceAbsolutePath = Path.GetFullPath(SourcePath);

        logger.Information("Watching for file changes in {SourceAbsolutePath}", SourceAbsolutePath);

        var fileWatcher = new FileSystemWatcher
        {
            Path = SourceAbsolutePath,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        // Subscribe to the desired events
        fileWatcher.Changed += OnSourceFileChanged;
        fileWatcher.Created += OnSourceFileChanged;
        fileWatcher.Deleted += OnSourceFileChanged;
        fileWatcher.Renamed += OnSourceFileChanged;
        return fileWatcher;
    }

    /// <summary>
    /// Restarts the server asynchronously.
    /// </summary>
    private async Task RestartServer()
    {
        // Check if another restart is already in progress
        await restartServerLock.WaitAsync();

        try
        {
            site = SiteHelper.Init(configFile, options, frontmatterParser, WhereParamsFilter, logger, stopwatch);

            // Stop the server
            if (host != null)
            {
                await host.StopAsync(TimeSpan.FromSeconds(1));
                host.Dispose();
            }
            await StartServer("http://localhost", 1122);
        }
        finally
        {
            _ = restartServerLock.Release();
        }
    }

    /// <summary>
    /// Handles the HTTP request asynchronously.
    /// </summary>
    /// <param name="context">The HttpContext representing the current request.</param>
    private async Task HandleRequest(HttpContext context)
    {
        var requestPath = context.Request.Path.Value;
        if (string.IsNullOrEmpty(Path.GetExtension(context.Request.Path.Value)) && (requestPath.Length > 1))
        {
            requestPath = requestPath.TrimEnd('/');
        }

        var fileAbsolutePath = Path.Combine(site.SourceStaticPath, requestPath.TrimStart('/'));
        var fileAbsoluteThemePath = Path.Combine(site.SourceThemeStaticPath, requestPath.TrimStart('/'));

        string? resultType;

        // Return the server startup timestamp as the response
        if (requestPath == "/ping")
        {
            resultType = "ping";
            await HandlePingRequest(context);
        }

        // Check if it is one of the Static files (serve the actual file)
        else if (File.Exists(fileAbsolutePath))
        {
            resultType = "static";
            await HandleStaticFileRequest(context, fileAbsolutePath);
        }

        // Check if it is one of the Static files (serve the actual file)
        else if (File.Exists(fileAbsoluteThemePath))
        {
            resultType = "themestatic";
            await HandleStaticFileRequest(context, fileAbsoluteThemePath);
        }

        // Check if the requested file path corresponds to a registered page
        else if (site.PagesDict.TryGetValue(requestPath, out var frontmatter))
        {
            resultType = "dict";
            await HandleRegisteredPageRequest(context, frontmatter);
        }

        else
        {
            resultType = "404";
            await HandleNotFoundRequest(context);
        }
        logger.Debug("Request {type}\tfor {RequestPath}", resultType, requestPath);
    }

    private Task HandlePingRequest(HttpContext context)
    {
        var content = serverStartTime.ToString("o");
        return context.Response.WriteAsync(content);
    }

    private async Task HandleStaticFileRequest(HttpContext context, string fileAbsolutePath)
    {
        context.Response.ContentType = GetContentType(fileAbsolutePath);
        using var fileStream = new FileStream(fileAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await fileStream.CopyToAsync(context.Response.Body);
    }

    private async Task HandleRegisteredPageRequest(HttpContext context, Frontmatter frontmatter)
    {
        var content = frontmatter.CreateOutputFile();
        content = InjectReloadScript(content);
        await context.Response.WriteAsync(content);
    }

    private async Task HandleNotFoundRequest(HttpContext context)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("404 - File Not Found");
    }

    /// <summary>
    /// Retrieves the content type of a file based on its extension.
    /// If the content type cannot be determined, the default value "application/octet-stream" is returned.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <returns>The content type of the file.</returns>
    private static string GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType ?? "application/octet-stream";
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
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("SuCoS.wwwroot.js.reload.js")
                ?? throw new FileNotFoundException("Could not find the embedded JavaScript resource.");
            using var reader = new StreamReader(stream);
            scriptContent = reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Could not read the JavaScript file.");
            throw;
        }

        // Inject the JavaScript content
        var reloadScript = $"<script>{scriptContent}</script>";

        const string bodyClosingTag = "</body>";
        content = content.Replace(bodyClosingTag, $"{reloadScript}{bodyClosingTag}", StringComparison.InvariantCulture);

        return content;
    }

    /// <summary>
    /// Handles the file change event from the file watcher.
    /// </summary>
    /// <param name="sender">The object that triggered the event.</param>
    /// <param name="e">The FileSystemEventArgs containing information about the file change.</param>
    private void OnSourceFileChanged(object sender, FileSystemEventArgs e)
    {
        // File changes are firing multiple events in a short time.
        // Debounce the event handler to prevent multiple events from firing in a short time
        debounceTimer?.Dispose();
        debounceTimer = new Timer(async _ =>
        {
            if (!restartInProgress)
            {
                logger.Information("File change detected: {FullPath}", e.FullPath);

                restartInProgress = true;
                await RestartServer();
                restartInProgress = false;
            }
        }, null, TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);
    }
}
