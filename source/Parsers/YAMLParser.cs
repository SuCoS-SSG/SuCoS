using System.Text;
using FolkerKinzel.Strings;
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
            .IgnoreUnmatchedProperties()
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
            throw new FormatException("Error parsing front matter");
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
