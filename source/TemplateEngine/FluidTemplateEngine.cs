using Fluid;
using Fluid.Values;
using Microsoft.Extensions.FileProviders;
using SuCoS.Models;

namespace SuCoS.TemplateEngine;

/// <summary>
/// Fluid template engine
/// </summary>
public class FluidTemplateEngine : ITemplateEngine
{
    /// <summary>
    /// The Fluid parser instance.
    /// </summary>
    private FluidParser FluidParser { get; } = new();

    /// <summary>
    /// The Fluid/Liquid template options.
    /// </summary>
    private TemplateOptions TemplateOptions { get; } = new();

    /// <summary>
    /// ctor
    /// </summary>
    public FluidTemplateEngine()
    {
        // Liquid template options, needed to theme the content
        // but also parse URLs
        TemplateOptions.MemberAccessStrategy.Register<Site>();
        TemplateOptions.MemberAccessStrategy.Register<Page>();
        TemplateOptions.MemberAccessStrategy.Register<Resource>();
        TemplateOptions.MemberAccessStrategy.Register<Theme>();

        // Liquid template options, needed to theme the content
        // but also parse URLs
        TemplateOptions.Filters.AddFilter("whereParams", WhereParamsFilter);
    }

    /// <inheritdoc/>
    public void Initialize(Site site)
    {
        ArgumentNullException.ThrowIfNull(site);

        TemplateOptions.FileProvider = new PhysicalFileProvider(Path.GetFullPath(site.SourceThemePath));
    }

    /// <inheritdoc/>
    public string Parse(string data, ISite site, IPage page)
    {
        if (FluidParser.TryParse(data, out var template, out var error))
        {
            var context = new TemplateContext(TemplateOptions)
                .SetValue("site", site)
                .SetValue("page", page);
            return template.Render(context);
        }
        else
        {
            throw new FormatException(error);
        }
    }

    /// <inheritdoc/>
    public string? ParseResource(string? data, ISite site, IPage page, int counter)
    {
        if (string.IsNullOrEmpty(data) || !FluidParser.TryParse(data, out var templateFileName, out var errorFileName))
        {
            return null;
        }
        var context = new TemplateContext(TemplateOptions)
                .SetValue("site", site)
                .SetValue("page", page)
                .SetValue("counter", counter);
        return templateFileName.Render(context);
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
