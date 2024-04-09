using Serilog;
using SuCoS.Helpers;
using SuCoS.Models.CommandLineOptions;
using SuCoS.ServerHandlers;
using System.Net;

namespace SuCoS;

/// <summary>
/// Serve Command will live serve the site and watch any changes.
/// </summary>
public sealed class ServeCommand : BaseGeneratorCommand, IDisposable
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
    /// A SemaphoreSlim used to ensure that server restarts due to file changes occur sequentially, 
    /// not concurrently. This is necessary because a restart involves stopping the current server
    /// and starting a new one, which would not be thread-safe without some form of synchronization.
    /// </summary>
    // private readonly SemaphoreSlim restartServerLock = new(1, 1);
    private Task lastRestartTask = Task.CompletedTask;

    private HttpListener? listener;

    private IServerHandlers[]? handlers;

    private DateTime serverStartTime;

    private Task? loop;

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

        // Watch for file changes in the specified path
        var SourceAbsolutePath = Path.GetFullPath(options.Source);
        logger.Information("Watching for file changes in {SourceAbsolutePath}", SourceAbsolutePath);
        fileWatcher.Start(SourceAbsolutePath, OnSourceFileChanged);
    }

    /// <summary>
    /// Starts the server asynchronously.
    /// </summary>
    /// <param name="baseURL">The base URL for the server.</param>
    /// <param name="port">The port number for the server.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public void StartServer(string baseURL = baseURLDefault, int port = portDefault)
    {
        logger.Information("Starting server...");

        // Generate the build report
        stopwatch.LogReport(site.Title);

        serverStartTime = DateTime.UtcNow;

        handlers = [
            new PingRequests(),
            new StaticFileRequest(site.SourceStaticPath, false),
            new StaticFileRequest(site.Theme?.StaticFolder, true),
            new RegisteredPageRequest(site),
            new RegisteredPageResourceRequest(site)
        ];
        listener = new HttpListener();
        listener.Prefixes.Add($"{baseURL}:{port}/");
        listener.Start();

        logger.Information("You site is live: {baseURL}:{port}", baseURL, port);

        loop = Task.Run(async () =>
        {
            while (listener is not null && listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync().ConfigureAwait(false);
                    await HandleRequest(context).ConfigureAwait(false);
                }
                catch (HttpListenerException ex)
                {
                    if (listener.IsListening)
                    {
                        logger.Error(ex, "Unexpected listener error.");
                    }
                    break;
                }
                catch (Exception ex)
                {
                    if (listener.IsListening)
                    {
                        logger.Error(ex, "Error processing request.");
                    }
                    break;
                }
            }
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        listener?.Stop();
        listener?.Close();
        fileWatcher.Stop();
        debounceTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Restarts the server asynchronously.
    /// </summary>
    private async Task RestartServer()
    {
        _ = await lastRestartTask.ContinueWith(async _ =>
        {
            logger.Information($"Restarting server...");

            if (listener != null && listener.IsListening)
            {
                listener.Stop();
                listener.Close();

                if (loop is not null)
                {
                    // Wait for the loop to finish processing any ongoing requests.
                    await loop.ConfigureAwait(false);
                    loop.Dispose();
                }
            }

            // Reinitialize the site
            site = SiteHelper.Init(configFile, options, Parser, WhereParamsFilter, logger, stopwatch);

            StartServer(baseURLDefault, portDefault);
        }).ConfigureAwait(false);

        lastRestartTask = lastRestartTask.ContinueWith(t => t.Exception != null
            ? throw t.Exception
            : Task.CompletedTask);
    }

    /// <summary>
    /// Handles the HTTP request asynchronously.
    /// </summary>
    /// <param name="context">The HttpContext representing the current request.</param>
    private async Task HandleRequest(HttpListenerContext context)
    {
        var requestPath = context.Request.Url?.AbsolutePath ?? string.Empty;

        string? resultType = null;
        if (handlers is not null)
        {
            var response = new HttpListenerResponseWrapper(context.Response);
            foreach (var item in handlers)
            {
                if (!item.Check(requestPath))
                {
                    continue;
                }

                try
                {
                    resultType = await item.Handle(response, requestPath, serverStartTime).ConfigureAwait(false);
                    break;
                }
                catch (Exception ex)
                {
                    logger.Debug(ex, "Error handling the request.");
                }
            }
        }

        if (resultType is null)
        {
            resultType = "404";
            await HandleNotFoundRequest(context).ConfigureAwait(true);
        }
        else
        {
            context.Response.OutputStream.Close();
        }
        logger.Debug("Request {type}\tfor {RequestPath}", resultType, requestPath);
    }

    private static async Task HandleNotFoundRequest(HttpListenerContext context)
    {
        context.Response.StatusCode = 404;
        using var writer = new StreamWriter(context.Response.OutputStream);
        await writer.WriteAsync("404 - File Not Found").ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the file change event from the file watcher.
    /// </summary>
    /// <param name="sender">The object that triggered the event.</param>
    /// <param name="e">The FileSystemEventArgs containing information about the file change.</param>
    private void OnSourceFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Contains("\\.git\\", StringComparison.InvariantCulture)) return;

        // File changes are firing multiple events in a short time.
        // Debounce the event handler to prevent multiple events from firing in a short time
        debounceTimer?.Dispose();
        debounceTimer = new Timer(DebounceCallback, e, TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);
    }

    private async void DebounceCallback(object? state)
    {
        await RestartServer().ConfigureAwait(false);
    }
}
