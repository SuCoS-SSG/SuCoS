using Fluid;
using Fluid.Values;
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
    protected IMetadataParser frontMatterParser { get; } = new YAMLParser();

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

        site = SiteHelper.Init(configFile, options, frontMatterParser, WhereParamsFilter, logger, stopwatch);
    }

    /// <summary>
    /// Fluid/Liquid filter to navigate Params dictionary
    /// </summary>
    /// <param name="input"></param>
    /// <param name="arguments"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    protected static ValueTask<FluidValue> WhereParamsFilter(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(arguments);

        List<FluidValue> result = [];
        var list = (input as ArrayValue)!.Values;

        var keys = arguments.At(0).ToStringValue().Split('.');
        foreach (var item in list)
        {
            if (item.ToObjectValue() is IParams param && CheckValueInDictionary(keys, param.Params, arguments.At(1).ToStringValue()))
            {
                result.Add(item);
            }
        }

        return new ArrayValue(result);
    }

    private static bool CheckValueInDictionary(string[] array, IReadOnlyDictionary<string, object> dictionary, string value)
    {
        var currentDictionary = dictionary;
        for (var i = 0; i < array.Length; i++)
        {
            var key = array[i];

            if (!currentDictionary.TryGetValue(key, out var dictionaryValue))
            {
                return false;
            }

            if (i == array.Length - 1)
            {
                return dictionaryValue.Equals(value);
            }

            if (dictionaryValue is not Dictionary<string, object> nestedDictionary)
            {
                return false;
            }

            currentDictionary = nestedDictionary;
        }
        return false;
    }
}
