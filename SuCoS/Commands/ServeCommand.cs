using System.Net;
using Serilog;
using SuCoS.Helpers;
using SuCoS.Models.CommandLineOptions;
using SuCoS.ServerHandlers;

namespace SuCoS.Commands;

/// <summary>
/// Serve Command will live serve the site and watch any changes.
/// </summary>
public sealed class ServeCommand : BaseGeneratorCommand, IDisposable
{
    /// <summary>
    /// Default values for the ServeCommand
    /// </summary>
    public const string BaseUrlDefault = "http://localhost";

    /// <summary>
    /// Default values for the ServeCommand
    /// </summary>
    public const int PortDefault = 2341;

    /// <summary>
    /// Default values for the ServeCommand
    /// </summary>
    public const int MaxPortTries = 10;

    /// <summary>
    /// The actual port being used after potential port selection
    /// </summary>
    public int PortUsed { get; private set; }

    /// <summary>
    /// The ServeOptions object containing the configuration parameters for the server.
    /// This includes settings such as the source directory to watch for file changes,
    /// verbosity of the logs, etc. These options are passed into the ServeCommand at construction
    /// and are used throughout its operation.
    /// </summary>
    private readonly ServeOptions _options;

    /// <summary>
    /// The FileSystemWatcher object that monitors the source directory for file changes.
    /// When a change is detected, this triggers a server restart to ensure the served content
    /// remains up-to-date. The FileSystemWatcher is configured with the source directory
    /// at construction and starts watching immediately.
    /// </summary>
    private readonly IFileWatcher _fileWatcher;

    private readonly IPortSelector _portSelector;

    /// <summary>
    /// A Timer that helps to manage the frequency of server restarts.
    /// When a file change is detected, the timer is reset. The action (server restart) only occurs
    /// when the timer expires, helping to ensure that rapid consecutive file changes result
    /// in a single server restart, not multiple.
    /// </summary>
    private Timer? _debounceTimer;

    /// <summary>
    /// A SemaphoreSlim used to ensure that server restarts due to file changes occur sequentially,
    /// not concurrently. This is necessary because a restart involves stopping the current server
    /// and starting a new one, which would not be thread-safe without some form of synchronization.
    /// </summary>
    private Task _lastRestartTask = Task.CompletedTask;

    private HttpListener? _listener;

    private IServerHandlers[]? _handlers;

    private DateTime _serverStartTime;

    private Task? _loop;

    private (WatcherChangeTypes changeType, string fullPath, DateTime dateTime) _lastFileChanged;

    /// <summary>
    /// Constructor for the ServeCommand class.
    /// </summary>
    /// <param name="options">ServeOptions object specifying the serve options.</param>
    /// <param name="logger">The logger instance. Injectable for testing</param>
    /// <param name="fileWatcher"></param>
    /// <param name="fs"></param>
    /// <param name="portSelector"></param>
    public ServeCommand(
        ServeOptions options,
        ILogger logger,
        IFileWatcher fileWatcher,
        IFileSystem fs,
        IPortSelector portSelector)
        : base(options, logger, fs)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
        _portSelector = portSelector ?? throw new ArgumentNullException(nameof(portSelector));

        var sourceAbsolutePath = Path.GetFullPath(options.Source);
        logger.Information("Watching for file changes in {SourceAbsolutePath}", sourceAbsolutePath);
        fileWatcher.Start(sourceAbsolutePath, OnSourceFileChanged);

        Site.SuCoS.IsServer = true;
    }

    /// <summary>
    /// Starts the server with intelligent port selection
    /// </summary>
    /// <param name="baseUrl">The base URL for the server.</param>
    /// <param name="requestedPort">The port number for the server.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public void StartServer(string baseUrl = "", int requestedPort = 0)
    {
        // TODO: baseUrl and requestedPort should be configurable
        baseUrl = BaseUrlDefault;
        requestedPort = PortDefault;
        Logger.Information("Starting server...");

        Stopwatch.LogReport(Site.Title);
        _serverStartTime = DateTime.UtcNow;

        PortUsed = _portSelector.SelectAvailablePort(baseUrl, requestedPort, MaxPortTries);

        _handlers = [
            new PingRequests(),
            new StaticFileRequest(Site.SourceStaticPath, false),
            new StaticFileRequest(Site.Theme?.StaticFolder, true),
            new RegisteredPageRequest(Site),
            new RegisteredPageResourceRequest(Site)
        ];

        _listener = new HttpListener();
        _listener.Prefixes.Add($"{baseUrl}:{PortUsed}/");
        _listener.Start();

        Logger.Information("Your site is live: {baseURL}:{port}", baseUrl, PortUsed);

        _loop = Task.Run(async () =>
        {
            while (_listener is not null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync().ConfigureAwait(false);
                    await HandleRequest(context).ConfigureAwait(false);
                }
                catch (HttpListenerException ex) when (_listener.IsListening)
                {
                    Logger.Error(ex, "Unexpected listener error.");
                    break;
                }
                catch (Exception ex) when (_listener.IsListening)
                {
                    Logger.Error(ex, "Error processing request.");
                    break;
                }
            }
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _listener?.Stop();
        _listener?.Close();
        _fileWatcher.Stop();
        _debounceTimer?.Dispose();
    }

    /// <summary>
    /// Restarts the server asynchronously.
    /// </summary>
    private async Task RestartServer()
    {
        _ = await _lastRestartTask.ContinueWith(async _ =>
        {
            Logger.Information("Restarting server...");

            if (_listener is { IsListening: true })
            {
                _listener.Stop();
                _listener.Close();

                if (_loop is not null)
                {
                    // Wait for the loop to finish processing any ongoing requests.
                    await _loop.ConfigureAwait(false);
                    _loop.Dispose();
                }
            }

            // Reinitialize the site
            Site = SiteHelper.Init(ConfigFile, _options, Parser, Logger, Stopwatch, Fs);

            StartServer();
        }).ConfigureAwait(false);

        _lastRestartTask = _lastRestartTask.ContinueWith(t => t.Exception != null
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
        if (_handlers is not null)
        {
            var response = new HttpListenerResponseWrapper(context.Response);
            foreach (var item in _handlers)
            {
                if (!item.Check(requestPath))
                {
                    continue;
                }

                try
                {
                    resultType = await item.Handle(response, requestPath, _serverStartTime).ConfigureAwait(false);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Error handling the request.");
                }
            }
        }

        if (resultType is null)
        {
            resultType = "404";
            await HandleNotFoundRequest(context).ConfigureAwait(false);
        }
        else
        {
            context.Response.OutputStream.Close();
        }
        Logger.Debug("Request {type}\tfor {RequestPath}", resultType, requestPath);
    }

    private static async Task HandleNotFoundRequest(HttpListenerContext context)
    {
        context.Response.StatusCode = 404;
        await using var writer = new StreamWriter(context.Response.OutputStream);
        await writer.WriteAsync("404 - File Not Found 22").ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the file change event from the file watcher.
    /// </summary>
    /// <param name="sender">The object that triggered the event.</param>
    /// <param name="e">The FileSystemEventArgs containing information about the file change.</param>
    private void OnSourceFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.Contains(@".git", StringComparison.InvariantCulture))
        {
            return;
        }

        if (_lastFileChanged.fullPath == e.FullPath
            && e.ChangeType == _lastFileChanged.changeType
            && (DateTime.Now - _lastFileChanged.dateTime).TotalMilliseconds < 150)
        {
            return;
        }

        _lastFileChanged = (e.ChangeType, e.FullPath, DateTime.Now);

        // File changes are firing multiple events in a short time.
        // Debounce the event handler to prevent multiple events from firing in a short time
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(DebounceCallback, e, TimeSpan.FromMilliseconds(1), Timeout.InfiniteTimeSpan);
    }

    private async void DebounceCallback(object? _)
    {
        await RestartServer().ConfigureAwait(false);
    }
}
