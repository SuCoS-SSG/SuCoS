using System.Text;
using FolkerKinzel.Strings;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SuCoS.Parsers;

/// <summary>
/// Responsible for parsing the content front matter using YAML
/// </summary>
public class YamlParser : IFrontMatterParser
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
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithCaseInsensitivePropertyMatching()
            .Build();
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
    public void SerializeAndSave<T>(T data, string fileFullPath)
    {
        var serializer = new SerializerBuilder()
        .IgnoreFields()
        .ConfigureDefaultValuesHandling(
            DefaultValuesHandling.OmitEmptyCollections
            | DefaultValuesHandling.OmitDefaults
            | DefaultValuesHandling.OmitNull)
        .Build();
        var dataString = serializer.Serialize(data);
        File.WriteAllText(fileFullPath, dataString);
    }

    /// <inheritdoc/>
    public (string, string) SplitFrontMatterAndContent(in string fileContent)
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
