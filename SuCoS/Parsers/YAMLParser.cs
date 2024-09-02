using System.Runtime.Serialization;
using System.Text;
using FolkerKinzel.Strings;
using Serilog;
using YamlDotNet.Serialization;

namespace SuCoS.Parsers;

/// <summary>
/// Responsible for parsing the content front matter using YAML
/// </summary>
public class YamlParser : IMetadataParser
{
    /// <summary>
    /// YamlDotNet parser, strictly set to allow automatically parse only known fields
    /// </summary>
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// ctor
    /// </summary>
    public YamlParser()
    {
        _deserializer = new StaticDeserializerBuilder(new StaticAotContext())
            .WithTypeConverter(new ParamsConverter())
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .WithTypeInspector(n => new IgnoreCaseTypeInspector(n))
            .IgnoreUnmatchedProperties()
            .Build();
    }

    private class IgnoreCaseTypeInspector(ITypeInspector innerTypeInspector) : ITypeInspector
    {
        private readonly ITypeInspector _innerTypeInspector = innerTypeInspector ?? throw new ArgumentNullException(nameof(innerTypeInspector));

        public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) => _innerTypeInspector.GetProperties(type, container ?? null);

        /// <inheritdoc/>
        public string GetEnumName(Type enumType, string name) => _innerTypeInspector.GetEnumName(enumType, name);

        /// <inheritdoc/>
        public string GetEnumValue(object enumValue) => _innerTypeInspector.GetEnumValue(enumValue);

        /// <inheritdoc/>
        public IPropertyDescriptor? GetProperty(Type type, object? container, string name, bool ignoreUnmatched, bool caseInsensitivePropertyMatching)
        {
            var candidates = GetProperties(type, container)
                .Where(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();

            var property = candidates.FirstOrDefault();

            if (property == null)
            {
                if (ignoreUnmatched)
                {
                    return null;
                }

                throw new SerializationException($"Property '{name}' not found on type '{type.FullName}'.");
            }

            if (candidates.Count > 1)
            {
                throw new SerializationException(
                    $"Multiple properties with the name/alias '{name}' already exists on type '{type.FullName}', maybe you're misusing YamlAlias or maybe you are using the wrong naming convention? The matching properties are: {string.Join(", ", candidates.Select(p => p.Name))}"
                );
            }

            return property;
        }
    }


    /// <inheritdoc/>
    public T Parse<T>(string content)
    {
        try
        {
            return _deserializer.Deserialize<T>(content);
        }
        catch
        {
            // TODO: Log original error
            throw new FormatException($"Error parsing YAML for '{typeof(T).Name}'");
        }
    }

    /// <inheritdoc/>
    public void Export<T>(T data, string path)
    {
        var serializer = new SerializerBuilder()
        .IgnoreFields()
        .ConfigureDefaultValuesHandling(
            DefaultValuesHandling.OmitEmptyCollections
            | DefaultValuesHandling.OmitDefaults
            | DefaultValuesHandling.OmitNull)
        .Build();
        var dataString = serializer.Serialize(data);
        File.WriteAllText(path, dataString);
    }

    /// <inheritdoc/>
    public (string, string) SplitFrontMatter(in string fileContent)
    {
        using var content = new StringReader(fileContent);
        var frontMatterBuilder = new StringBuilder();
        string? line;

        // find the start of the block
        while ((line = content.ReadLine()) != null && line != "---") { }
        // find the end of the block
        while ((line = content.ReadLine()) != null && line != "---")
        {
            _ = frontMatterBuilder.AppendLine(line);
        }
        frontMatterBuilder.TrimEnd();

        // Join the read lines to form the front matter
        var yaml = frontMatterBuilder.ToString();
        var rawContent = content.ReadToEnd();

        return (yaml, rawContent);
    }
}
