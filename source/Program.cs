using System.CommandLine;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;

namespace SuCoS;

/// <summary>
/// The main entry point of the program.
/// </summary>
public class Program
{
    private static int Main(string[] args)
    {
        // use Serilog to log the program's output
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
            .CreateLogger();

        // Print the logo of the program.
        OutputLogo();

        // Print the name and version of the program.
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName();
        var appName = assemblyName?.Name;
        var appVersion = assemblyName?.Version;
        Log.Information("{name} v{version}", appName, appVersion);

        // Shared options between the commands
        var sourceOption = new Option<string>(new[] { "--source", "-s" }, () => ".", "Source directory path");
        var futureOption = new Option<bool>(new[] { "--future", "-f" }, () => false, "Include content with dates in the future");
        var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, () => false, "Verbose output");

        // BuildCommand setup
        var buildOutputOption = new Option<string>(new[] { "--output", "-o" }, () => "./public", "Output directory path");

        Command buildCommand = new("build", "Builds the site")
        {
            sourceOption,
            buildOutputOption,
            futureOption,
            verboseOption
        };
        buildCommand.SetHandler((source, output, future, verbose) =>
        {
            BuildOptions buildOptions = new()
            {
                Source = source,
                Output = output,
                Future = future
            };
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(verbose ? LogEventLevel.Debug : LogEventLevel.Information)
                .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
                .CreateLogger();
            _ = new BuildCommand(buildOptions);
        },
        sourceOption, buildOutputOption, futureOption, verboseOption);

        // ServerCommand setup
        Command serveCommand = new("serve", "Starts the server")
        {
            sourceOption,
            futureOption,
            verboseOption
        };
        serveCommand.SetHandler(async (source, future, verbose) =>
        {
            ServeOptions serverOptions = new()
            {
                Source = source,
                Future = future
            };
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(verbose ? LogEventLevel.Debug : LogEventLevel.Information)
                .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
                .CreateLogger();

            var serveCommand = new ServeCommand(serverOptions);
            await serveCommand.RunServer();
            await Task.Delay(-1);  // Wait forever.
        },
        sourceOption, futureOption, verboseOption);

        RootCommand rootCommand = new("SuCoS commands")
        {
            buildCommand,
            serveCommand
        };

        return rootCommand.Invoke(args);
    }

    private static void OutputLogo()
    {
        Log.Information(@"
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
