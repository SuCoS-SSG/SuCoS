using Serilog;
using SuCoS.Helpers;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parser;

namespace SuCoS;

/// <summary>
/// Base class for build and serve commands.
/// </summary>
public abstract class BaseGeneratorCommand
{
    /// <summary>
    /// The configuration file name.
    /// </summary>
    protected const string configFile = "sucos.yaml";

    /// <summary>
    /// The site configuration.
    /// </summary>
    protected Site site { get; set; }

    /// <summary>
    /// The front matter parser instance. The default is YAML.
    /// </summary>
    protected IMetadataParser Parser { get; } = new YAMLParser();

    /// <summary>
    /// The stopwatch reporter.
    /// </summary>
    protected StopwatchReporter stopwatch { get; }

    /// <summary>
    /// The logger (Serilog).
    /// </summary>
    protected ILogger logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGeneratorCommand"/> class.
    /// </summary>
    /// <param name="options">The generate options.</param>
    /// <param name="logger">The logger instance. Injectable for testing</param>
    protected BaseGeneratorCommand(IGenerateOptions options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        stopwatch = new(logger);

        logger.Information("Source path: {source}", propertyValue: options.Source);

        site = SiteHelper.Init(configFile, options, Parser, logger, stopwatch);
    }
}
