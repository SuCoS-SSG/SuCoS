using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
    private readonly FileSystemWatcher fileWatcher;

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

    /// <summary>
    /// A dictionary mapping route paths (e.g., "about") to functions that generate the corresponding 
    /// page content. This could be replaced with more complex logic, such as loading the content 
    /// from .html files.
    /// </summary>
    private readonly Dictionary<string, Frontmatter> pages = new();

    private DateTime serverStartTime;

    /// <summary>
    /// Constructor for the ServeCommand class.
    /// </summary>
    /// <param name="options">ServeOptions object specifying the serve options.</param>
    public ServeCommand(ServeOptions options) : base(options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        var baseURL = "http://localhost";
        var port = 1122;
        baseURLGlobal = $"{baseURL}:{port}";

        // Watch for file changes in the specified path
        fileWatcher = StartFileWatcher(options.Source);
    }

    private void CreatePagesDictionary()
    {
        try
        {
            site = ReadAppConfig(options: options, frontmatterParser);
            if (site is null)
            {
                throw new FormatException("Error reading app config");
            }
        }
        catch
        {
            throw new FormatException("Error reading app config");
        }

        ScanAllMarkdownFiles();

        ParseSourceFiles();

        // Generate the build report
        stopwatch.LogReport(site.Title);

        pages.Clear();

        foreach (var Frontmatter in site.Pages)
        {
            if (Frontmatter.Permalink != null)
            {
                pages.TryAdd(Frontmatter.Permalink, Frontmatter);
                if (Path.GetFileName(Frontmatter.Permalink) == "index.html")
                {
                    var path = Path.GetDirectoryName(Frontmatter.Permalink);
                    pages.TryAdd(path!, Frontmatter);
                }
            }
        }
    }

    /// <summary>
    /// Starts the file watcher to monitor file changes in the specified source path.
    /// </summary>
    /// <param name="SourcePath">The path to the source directory.</param>
    /// <returns>The created FileSystemWatcher object.</returns>
    private FileSystemWatcher StartFileWatcher(string SourcePath)
    {
        var SourceAbsolutePath = Path.GetFullPath(SourcePath);

        Log.Information("Watching for file changes in {SourceAbsolutePath}", SourceAbsolutePath);

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
        if (options.Verbose || true)
        {
            Log.Information("Starting server...");
        }

        CreatePagesDictionary();

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
        Log.Information("You site is live: {baseURL}:{port}", baseURL, port);

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
            Log.Information("Restarting server...");

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
            restartServerLock.Release();
        }
    }

    /// <summary>
    /// Handles the HTTP request asynchronously.
    /// </summary>
    /// <param name="context">The HttpContext representing the current request.</param>
    private async Task HandleRequest(HttpContext context)
    {
        var requestPath = context.Request.Path.Value.TrimStart('/');
        var fileAbsolutePath = Path.Combine(options.Source, "static", requestPath);

        var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var wwwfileAbsolutePath = Path.Combine(wwwrootPath, requestPath);

        if (options.Verbose)
        {
            Log.Information("Request received for {RequestPath}", requestPath);
        }

        // Return the server startup timestamp as the response
        if (requestPath == "ping")
        {
            var timestamp = serverStartTime.ToString("o");
            await context.Response.WriteAsync(timestamp);
        }

        // Check if the requested file path is not empty
        else if (File.Exists(wwwfileAbsolutePath))
        {
            // Set the content type header
            context.Response.ContentType = GetContentType(wwwfileAbsolutePath); ;

            using var fileStream = new FileStream(wwwfileAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // Copy the file stream to the response body
            await fileStream.CopyToAsync(context.Response.Body);
        }

        // Check if the requested file path is not empty
        else if (File.Exists(fileAbsolutePath))
        {
            // Set the content type header
            context.Response.ContentType = GetContentType(fileAbsolutePath);

            using var fileStream = new FileStream(fileAbsolutePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // Copy the file stream to the response body
            await fileStream.CopyToAsync(context.Response.Body);
        }

        // Check if the requested file path corresponds to a registered page
        else if (pages.TryGetValue(requestPath, out var frontmatter))
        {
            // Generate the output content for the frontmatter
            var content = CreateOutputFile(frontmatter);

            // Inject JavaScript snippet for reloading the page if it was restarted
            content = InjectReloadScript(content);

            // Write the generated content to the response
            await context.Response.WriteAsync(content);
        }

        else
        {
            // The requested file was not found
            // Set the response status code to 404 and write a corresponding message
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("404 - File Not Found");
        }
    }

    private string GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }
        return contentType ?? "application/octet-stream";
    }

    private static string InjectReloadScript(string content)
    {
        // Inject a reference to the JavaScript file
        const string reloadScript = "<script src=\"/js/reload.js\"></script>";

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
                Log.Information("File change detected: {FullPath}", e.FullPath);

                restartInProgress = true;
                await RestartServer();
                restartInProgress = false;
            }
        }, null, TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        host?.Dispose();
        fileWatcher?.Dispose();
        debounceTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
