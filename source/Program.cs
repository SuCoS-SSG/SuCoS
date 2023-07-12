using System.CommandLine;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS;

/// <summary>
/// The main entry point of the program.
/// </summary>
public class Program
{
    private ILogger logger;

    /// <summary>
    /// Constructor
    /// </summary>
    private Program(ILogger logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Entry point of the program
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static int Main(string[] args)
    {
        ILogger logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
            .CreateLogger();

        var program = new Program(logger);
        return program.Run(args);
    }

    private int Run(string[] args)
    {
        // Print the logo of the program.
        OutputLogo();

        // Print the name and version of the program.
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName();
        var appName = assemblyName?.Name;
        var appVersion = assemblyName?.Version;
        logger.Information("{name} v{version}", appName, appVersion);

        // Shared options between the commands
        var sourceOption = new Option<string>(new[] { "--source", "-s" }, () => ".", "Source directory path");
        var futureOption = new Option<bool>(new[] { "--future", "-f" }, "Include content with dates in the future");
        var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, "Verbose output");

        // BuildCommand setup
        var buildOutputOption = new Option<string>(new[] { "--output", "-o" }, "Output directory path");

        Command buildCommandHandler = new("build", "Builds the site")
        {
            sourceOption,
            buildOutputOption,
            futureOption,
            verboseOption
        };
        buildCommandHandler.SetHandler((source, output, future, verbose) =>
        {
            BuildOptions buildOptions = new(
                output: string.IsNullOrEmpty(output) ? Path.Combine(source, "public") : output)
            {
                Source = source,
                Future = future
            };
            logger = new LoggerConfiguration()
                .MinimumLevel.Is(verbose ? LogEventLevel.Debug : LogEventLevel.Information)
                .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
                .CreateLogger();
            _ = new BuildCommand(buildOptions, logger);
        },
        sourceOption, buildOutputOption, futureOption, verboseOption);

        // ServerCommand setup
        Command serveCommandHandler = new("serve", "Starts the server")
        {
            sourceOption,
            futureOption,
            verboseOption
        };
        serveCommandHandler.SetHandler(async (source, future, verbose) =>
        {
            ServeOptions serverOptions = new()
            {
                Source = source,
                Future = future
            };
            logger = new LoggerConfiguration()
                .MinimumLevel.Is(verbose ? LogEventLevel.Debug : LogEventLevel.Information)
                .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
                .CreateLogger();

            var serveCommand = new ServeCommand(serverOptions, logger);
            await serveCommand.RunServer();
            await Task.Delay(-1);  // Wait forever.
        },
        sourceOption, futureOption, verboseOption);

        RootCommand rootCommand = new("SuCoS commands")
        {
            buildCommandHandler,
            serveCommandHandler
        };

        return rootCommand.Invoke(args);
    }

    private void OutputLogo()
    {
        logger.Information(@"
 ____             ____            ____       
/\  _`\          /\  _`\         /\  _`\     
\ \,\L\_\  __  __\ \ \/\_\    ___\ \,\L\_\   
 \/_\__ \ /\ \/\ \\ \ \/_/_  / __`\/_\__ \   
   /\ \L\ \ \ \_\ \\ \ \L\ \/\ \L\ \/\ \L\ \ 
   \ `\____\ \____/ \ \____/\ \____/\ `\____\
    \/_____/\/___/   \/___/  \/___/  \/_____/
");
    }
}
