using System.CommandLine;
using System.Reflection;
using Serilog;

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
            .MinimumLevel.Debug()
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
            .CreateLogger();

        // Print the logo of the program.
        OutputLogo();

        // Print the name and version of the program.git remote add origin git@gitlab.com:brmassa/sucos.git
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName();
        var appName = assemblyName?.Name;
        var appVersion = assemblyName?.Version;
        Log.Information("{name} v{version}", appName, appVersion);

        var buildSourceOption = new Option<string>(new[] { "--source", "-s" }, () => ".", "Source directory path");
        var buildOutputOption = new Option<string>(new[] { "--output", "-o" }, () => "./public", "Output directory path");
        var buildVerboseOption = new Option<bool>(new[] { "--verbose", "-v" }, () => false, "Verbose output");
        var buildCommand = new RootCommand
            {
                buildSourceOption,
                buildOutputOption,
                buildVerboseOption
            };
        buildCommand.Description = "Build commands";
        buildCommand.SetHandler((source, output, verbose) =>
            {
                var buildOptions = new BuildOptions()
                {
                    Source = source,
                    Output = output,
                    Verbose = verbose
                };
                var build = new BuildCommand(buildOptions);
            },
            buildSourceOption, buildOutputOption, buildVerboseOption);

        return buildCommand.Invoke(args);
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

        //         Log.Information(@"
        // Static sie generator
        // using
        // C
        // o
        // Sharp
        // ");
    }
}
