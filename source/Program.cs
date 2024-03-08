using Serilog;
using Serilog.Events;
using SuCoS.Helpers;
using SuCoS.Models.CommandLineOptions;
using System.Reflection;
using CommandLine;

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
    public const string helloWorld = @"
 ____             ____            ____       
/\  _`\          /\  _`\         /\  _`\     
\ \,\L\_\  __  __\ \ \/\_\    ___\ \,\L\_\   
 \/_\__ \ /\ \/\ \\ \ \/_/_  / __`\/_\__ \   
   /\ \L\ \ \ \_\ \\ \ \L\ \/\ \L\ \/\ \L\ \ 
   \ `\____\ \____/ \ \____/\ \____/\ `\____\
    \/_____/\/___/   \/___/  \/___/  \/_____/
";

    private ILogger Logger { get; set; } = logger;
    private static readonly string[] aliases = ["--source", "-s"];
    private static readonly string[] aliasesArray = ["--draft", "-d"];
    private static readonly string[] aliasesArray0 = ["--future", "-f"];
    private static readonly string[] aliasesArray1 = ["--expired", "-e"];
    private static readonly string[] aliasesArray2 = ["--verbose", "-v"];
    private static readonly string[] aliasesArray3 = ["--output", "-o"];

    /// <summary>
    /// Entry point of the program
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task<int> Main(string[] args)
    {
        var program = new Program(CreateLogger());
        return await program.RunCommandLine(args);
    }

    /// <summary>
    /// Actual entrypoint of the program
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public async Task<int> RunCommandLine(string[] args)
    {
        return await CommandLine.Parser.Default.ParseArguments<BuildOptions, ServeOptions>(args)
            .WithParsed<GenerateOptions>(options =>
            {
                Logger = CreateLogger(options.Verbose);
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
                        _ = new BuildCommand(options, Logger);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Build failed: {ex.Message}");
                        return Task.FromResult(1);
                    }
                    return Task.FromResult(0);
                },
                async (ServeOptions options) =>
                {
                    try
                    {
                        var serveCommand = new ServeCommand(options, Logger, new SourceFileWatcher());
                        serveCommand.StartServer();
                        await Task.Delay(-1).ConfigureAwait(false);  // Wait forever.
                    }
                    catch (Exception ex)
                    {
                        if (options.Verbose)
                        {
                            Logger.Error(ex, "Serving failed");
                        }
                        else
                        {
                            Logger.Error($"Serving failed: {ex.Message}");
                        }
                        return 1;
                    }
                    return 0;
                }
                , errs => Task.FromResult(1)
                );
    }

    /// <summary>
    /// Create a log (normally from Serilog), depending the verbose option
    /// </summary>
    /// <param name="verbose"></param>
    /// <returns></returns>
    public static ILogger CreateLogger(bool verbose = false)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(verbose ? LogEventLevel.Debug : LogEventLevel.Information)
            // .WriteTo.Async(a => a.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture))
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
            .CreateLogger();
    }

    /// <summary>
    /// Print the name and version of the program.
    /// </summary>
    public void OutputWelcome()
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName();
        var appName = assemblyName?.Name;
        var appVersion = assemblyName?.Version;
        Logger.Information("{name} v{version}", appName, appVersion);
    }

    /// <summary>
    /// Print the logo
    /// </summary>
    public void OutputLogo()
    {
        Logger.Information(helloWorld);
    }
}
