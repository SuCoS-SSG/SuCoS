using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Serilog;
using SuCoS.Helpers;
using SuCoS.Models.CommandLineOptions;
using SuCoS.ServerHandlers;

namespace SuCoS;

/// <summary>
/// Serve Command will live serve the site and watch any changes.
/// </summary>
public class ServeCommand : BaseGeneratorCommand, IDisposable
{
    private const string baseURLDefault = "http://localhost";
    private const int portDefault = 1122;

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

    private IServerHandlers[]? handlers;

    /// <summary>
    /// The FileSystemWatcher object that monitors the source directory for file changes.
    /// When a change is detected, this triggers a server restart to ensure the served content 
    /// remains up-to-date. The FileSystemWatcher is configured with the source directory 
    /// at construction and starts watching immediately.
    /// </summary>
    private readonly IFileWatcher fileWatcher;

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
    private volatile bool restartInProgress;

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
    private async Task StartServer(string baseURL, int port)
    {
        logger.Information("Starting server...");

        // Generate the build report
        stopwatch.LogReport(site.Title);

        serverStartTime = DateTime.UtcNow;

        handlers = new IServerHandlers[]{
            new PingRequests(),
            new StaticFileRequest(site.SourceStaticPath, false),
            new StaticFileRequest(site.SourceThemeStaticPath, true),
            new RegisteredPageRequest(site),
            new RegisteredPageResourceRequest(site)
        };

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
        fileWatcher.Stop();
        debounceTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Constructor for the ServeCommand class.
    /// </summary>
    /// <param name="options">ServeOptions object specifying the serve options.</param>
    /// <param name="logger">The logger instance. Injectable for testing</param>
    /// <param name="fileWatcher"></param>
    public ServeCommand(ServeOptions options, ILogger logger, IFileWatcher fileWatcher) : base(options, logger)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
        baseURLGlobal = $"{baseURLDefault}:{portDefault}";

        // Watch for file changes in the specified path
        var SourceAbsolutePath = Path.GetFullPath(options.Source);
        logger.Information("Watching for file changes in {SourceAbsolutePath}", SourceAbsolutePath);
        fileWatcher.Start(SourceAbsolutePath, OnSourceFileChanged);
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
            site = SiteHelper.Init(configFile, options, frontMatterParser, WhereParamsFilter, logger, stopwatch);

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
        if (string.IsNullOrEmpty(Path.GetExtension(context.Request.Path.Value)) && requestPath.Length > 1)
        {
            requestPath = requestPath.TrimEnd('/');
        }

        string? resultType = null;
        if (handlers is not null)
        {
            foreach (var item in handlers)
            {
                if (item.Check(requestPath))
                {
                    resultType = await item.Handle(context, requestPath, serverStartTime);
                    break;
                }
            }
        }

        if (resultType is null)
        {
            resultType = "404";
            await HandleNotFoundRequest(context);
        }
        logger.Debug("Request {type}\tfor {RequestPath}", resultType, requestPath);
    }

    private static async Task HandleNotFoundRequest(HttpContext context)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("404 - File Not Found");
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
        debounceTimer = new Timer(DebounceCallback, e, TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);
    }

    private void DebounceCallback(object? state)
    {
        if (state is not FileSystemEventArgs e)
        {
            return;
        }
        HandleFileChangeAsync(e).GetAwaiter().GetResult();
    }

    private async Task HandleFileChangeAsync(FileSystemEventArgs e)
    {
        if (restartInProgress)
        {
            return;
        }

        logger.Information("File change detected: {FullPath}", e.FullPath);

        restartInProgress = true;
        try
        {
            await RestartServer();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to restart server.");
            throw;
        }
        finally
        {
            restartInProgress = false;
        }
    }
}
