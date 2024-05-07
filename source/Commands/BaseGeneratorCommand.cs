using Serilog;
using SuCoS.Helpers;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parsers;

namespace SuCoS.Commands;

/// <summary>
/// Base class for build and serve commands.
/// </summary>
public abstract class BaseGeneratorCommand
{
    /// <summary>
    /// The configuration file name.
    /// </summary>
    protected const string ConfigFile = "sucos.yaml";

    /// <summary>
    /// The site configuration.
    /// </summary>
    protected Site Site { get; set; }

    /// <summary>
    /// The front matter parser instance. The default is YAML.
    /// </summary>
    protected IMetadataParser Parser { get; } = new YamlParser();

    /// <summary>
    /// The stopwatch reporter.
    /// </summary>
    protected StopwatchReporter Stopwatch { get; }

    /// <summary>
    /// The logger (Serilog).
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// File system functions (file and directory)
    /// </summary>
    protected readonly IFileSystem Fs;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGeneratorCommand"/> class.
    /// </summary>
    /// <param name="options">The generate options.</param>
    /// <param name="logger">The logger instance. Injectable for testing</param>
    /// <param name="fs"></param>
    protected BaseGeneratorCommand(IGenerateOptions options, ILogger logger, IFileSystem fs)
    {
        ArgumentNullException.ThrowIfNull(options);

        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Stopwatch = new(logger);
        Fs = fs;

        logger.Information("Source path: {source}", propertyValue: options.Source);

        Site = SiteHelper.Init(ConfigFile, options, Parser, logger, Stopwatch, fs);
    }
}
