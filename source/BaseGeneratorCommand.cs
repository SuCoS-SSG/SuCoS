using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Serilog;
using SuCoS.Models;
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
    protected Site site;

    /// <summary>
    /// The frontmatter parser instance. The default is YAML.
    /// </summary>
    protected readonly IFrontmatterParser frontmatterParser = new YAMLParser();

    /// <summary>
    /// The stopwatch reporter.
    /// </summary>
    protected readonly StopwatchReporter stopwatch = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseGeneratorCommand"/> class.
    /// </summary>
    /// <param name="options">The generate options.</param>
    protected BaseGeneratorCommand(IGenerateOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        Log.Information("Source path: {source}", propertyValue: options.Source);

        site = SiteHelper.Init(configFile, options, frontmatterParser, WhereParamsFilter, stopwatch);
    }

    /// <summary>
    /// Fluid/Liquid filter to navigate Params dictionary
    /// </summary>
    /// <param name="input"></param>
    /// <param name="arguments"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueTask<FluidValue> WhereParamsFilter(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        List<FluidValue> result = new();
        var list = (input as ArrayValue)!.Values;

        var keys = arguments.At(0).ToStringValue().Split('.');
        foreach (var item in list)
        {
            if (item.ToObjectValue() is IParams param && CheckValueInDictionary(keys, param.Params, arguments.At(1).ToStringValue()))
            {
                result.Add(item);
            }
        }

        return new ValueTask<FluidValue>(new ArrayValue((IEnumerable<FluidValue>)result));
    }

    static bool CheckValueInDictionary(string[] array, Dictionary<string, object> dictionary, string value)
    {
        var key = array[0];

        // Check if the key exists in the dictionary
        if (dictionary.TryGetValue(key, out var dictionaryValue))
        {
            // If it's the last element in the array, check if the dictionary value matches the value parameter
            if (array.Length == 1)
            {
                return dictionaryValue.Equals(value);
            }

            // Check if the value is another dictionary
            else if (dictionaryValue is Dictionary<string, object> nestedDictionary)
            {
                // Create a new array without the current key
                var newArray = new string[array.Length - 1];
                Array.Copy(array, 1, newArray, 0, newArray.Length);

                // Recursively call the method with the nested dictionary and the new array
                return CheckValueInDictionary(newArray, nestedDictionary, value);
            }
        }

        // If the key doesn't exist or the value is not a dictionary, return false
        return false;
    }
}
