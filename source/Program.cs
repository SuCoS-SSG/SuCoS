﻿using System.CommandLine;
using System.Reflection;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS;

/// <summary>
/// The main entry point of the program.
/// </summary>
internal class Program
{
    internal const string helloWorld = @"
 ____             ____            ____       
/\  _`\          /\  _`\         /\  _`\     
\ \,\L\_\  __  __\ \ \/\_\    ___\ \,\L\_\   
 \/_\__ \ /\ \/\ \\ \ \/_/_  / __`\/_\__ \   
   /\ \L\ \ \ \_\ \\ \ \L\ \/\ \L\ \/\ \L\ \ 
   \ `\____\ \____/ \ \____/\ \____/\ `\____\
    \/_____/\/___/   \/___/  \/___/  \/_____/
";

    private ILogger logger;

    /// <summary>
    /// Entry point of the program
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static int Main(string[] args)
    {
        var logger = CreateLogger();
        var program = new Program(logger);
        return program.Run(args);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Program(ILogger logger)
    {
        this.logger = logger;
    }

    internal int Run(string[] args)
    {
        // Print the logo of the program.
        OutputLogo();
        OutputWelcome();

        // Shared options between the commands
        var sourceOption = new Option<string>(new[] { "--source", "-s" }, () => ".", "Source directory path");
        var futureOption = new Option<bool>(new[] { "--future", "-f" }, "Include content with dates in the future");
        var expiredOption = new Option<bool>(new[] { "--expired", "-e" }, "Include content with ExpiredDate dates from the past");
        var verboseOption = new Option<bool>(new[] { "--verbose", "-v" }, "Verbose output");

        // BuildCommand setup
        var buildOutputOption = new Option<string>(new[] { "--output", "-o" }, "Output directory path");

        Command buildCommandHandler = new("build", "Builds the site")
        {
            sourceOption,
            buildOutputOption,
            futureOption,
            expiredOption,
            verboseOption
        };
        buildCommandHandler.SetHandler((source, output, future, expired, verbose) =>
        {
            logger = CreateLogger(verbose);

            BuildOptions buildOptions = new(
                source: source,
                output: output)
            {
                Future = future,
                Expired = expired
            };
            _ = new BuildCommand(buildOptions, logger);
        },
        sourceOption, buildOutputOption, futureOption, expiredOption, verboseOption);

        // ServerCommand setup
        Command serveCommandHandler = new("serve", "Starts the server")
        {
            sourceOption,
            futureOption,
            expiredOption,
            verboseOption
        };
        serveCommandHandler.SetHandler(async (source, future, expired, verbose) =>
        {
            logger = CreateLogger(verbose);

            ServeOptions serverOptions = new()
            {
                Source = source,
                Future = future,
                Expired = expired
            };

            var serveCommand = new ServeCommand(serverOptions, logger, new SourceFileWatcher());
            await serveCommand.RunServer();
            await Task.Delay(-1);  // Wait forever.
        },
        sourceOption, futureOption, expiredOption, verboseOption);

        RootCommand rootCommand = new("SuCoS commands")
        {
            buildCommandHandler,
            serveCommandHandler
        };

        return rootCommand.Invoke(args);
    }

    internal static ILogger CreateLogger(bool verbose = false)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(verbose ? LogEventLevel.Debug : LogEventLevel.Information)
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.CurrentCulture)
            .CreateLogger();
    }

    /// <summary>
    /// Print the name and version of the program.
    /// </summary>
    internal void OutputWelcome()
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName();
        var appName = assemblyName?.Name;
        var appVersion = assemblyName?.Version;
        logger.Information("{name} v{version}", appName, appVersion);
    }

    internal void OutputLogo()
    {
        logger.Information(helloWorld);
    }
}
