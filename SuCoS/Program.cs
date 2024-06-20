using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using CommandLine;
using Serilog;
using Serilog.Events;
using SuCoS.Commands;
using SuCoS.Helpers;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS;

/// <summary>
/// The main entry point of the program.
/// </summary>
/// <remarks>
/// Constructor
/// </remarks>
public class Program(ILogger logger)
{
    /// <summary>
    /// Basic logo of the program, for fun
    /// </summary>
    private static readonly string HelloWorld = @"
░█▀▀░░░░░█▀▀░░░░░█▀▀
░▀▀█░█░█░█░░░█▀█░▀▀█
░▀▀▀░▀▀▀░▀▀▀░▀▀▀░▀▀▀";

    /// <summary>
    /// Entry point of the program
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task<int> Main(string[] args)
    {
        var program = new Program(CreateLogger());
        return await program.RunCommandLine(args).ConfigureAwait(false);
    }

    /// <summary>
    /// Actual entrypoint of the program
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GenerateOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BuildOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ServeOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CheckLinkOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NewSiteOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NewThemeOptions))]
    public async Task<int> RunCommandLine(string[] args)
    {
        OutputLogo();
        OutputWelcome();
        return await Parser.Default.ParseArguments<BuildOptions, ServeOptions, CheckLinkOptions, NewSiteOptions, NewThemeOptions>(args)
            .WithParsed<GenerateOptions>(options =>
            {
                logger = CreateLogger(options.Verbose);
            })
            .WithParsed<BuildOptions>(options =>
            {
                options.Output = string.IsNullOrEmpty(options.Output) ? Path.Combine(options.Source, "public") : options.Output;
            })
            .MapResult(
                (BuildOptions options) =>
                {
                    try
                    {
                        var command = new BuildCommand(options, logger, new FileSystem());
                        return Task.FromResult(command.Run());
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Build failed: {ex.Message}");
                        return Task.FromResult(1);
                    }
                },
                async (ServeOptions options) =>
                {
                    try
                    {
                        var serveCommand = new ServeCommand(options, logger, new SourceFileWatcher(), new FileSystem());
                        serveCommand.StartServer();
                        await Task.Delay(-1).ConfigureAwait(false);  // Wait forever.
                    }
                    catch (Exception ex)
                    {
                        if (options.Verbose)
                        {
                            logger.Error(ex, "Serving failed");
                        }
                        else
                        {
                            logger.Error($"Serving failed: {ex.Message}");
                        }
                        return 1;
                    }
                    return 0;
                },
                (CheckLinkOptions options) =>
                {
                    logger = CreateLogger(options.Verbose);
                    var command = new CheckLinkCommand(options, logger);
                    return command.Run();
                },
                (NewSiteOptions options) =>
                {
                    var fileSystem = new FileSystem();
                    var command = NewSiteCommand.Create(options, logger, fileSystem);
                    return Task.FromResult(command.Run());
                },
                (NewThemeOptions options) =>
                {
                    var command = new NewThemeCommand(options, logger);
                    return Task.FromResult(command.Run());
                },
                 _ => Task.FromResult(0)
                ).ConfigureAwait(false);
    }

    /// <summary>
    /// Create a log (normally from Serilog), depending on the verbose option
    /// </summary>
    /// <param name="verbose"></param>
    /// <returns></returns>
    public static ILogger CreateLogger(bool verbose = false) => new LoggerConfiguration()
        .MinimumLevel.Is(verbose ? LogEventLevel.Debug : LogEventLevel.Information)
        // .WriteTo.Async(a => a.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture))
        .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
        .CreateLogger();

    /// <summary>
    /// Print the name and version of the program.
    /// </summary>
    public void OutputWelcome()
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName();
        var appName = assemblyName?.Name;
        var appVersion = assemblyName?.Version;
        logger.Information("{name} v{version}", appName, appVersion);
    }

    /// <summary>
    /// Print the logo
    /// </summary>
    public void OutputLogo()
    {
        logger.Information(HelloWorld);
    }
}
