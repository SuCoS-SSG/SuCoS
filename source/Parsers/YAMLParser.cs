using System.Text;
using FolkerKinzel.Strings;
using SuCoS.Helpers;
using SuCoS.Models;
using YamlDotNet.Serialization;

namespace SuCoS.Parser;

/// <summary>
/// Responsible for parsing the content front matter using YAML
/// </summary>
public class YAMLParser : IMetadataParser
{
    /// <summary>
    /// YamlDotNet parser, strictly set to allow automatically parse only known fields
    /// </summary>
    private readonly IDeserializer deserializer;

    /// <summary>
    /// ctor
    /// </summary>
    public YAMLParser()
    {
        deserializer = new StaticDeserializerBuilder(new StaticAOTContext())
            .WithTypeConverter(new ParamsConverter())
            .IgnoreUnmatchedProperties()
            .Build();
    }

    // /// <inheritdoc/>
    // public IFrontMatter ParseFrontmatterAndMarkdown(
    //     in string fileFullPath,
    //     in string fileRelativePath,
    //     in string fileContent
    // )
    // {
    //     var (yaml, rawContent) = SplitFrontMatter(fileContent);

    //     // Now, you can parse the YAML front matter
    //     var page = ParseYAML(fileFullPath, fileRelativePath, yaml, rawContent);

    //     return page;
    // }

    // private FrontMatter ParseYAML(
    //     in string fileFullPath,
    //     in string fileRelativePath,
    //     string yaml,
    //     in string rawContent
    // )
    // {
    //     var frontMatter =
    //         deserializer.Deserialize<FrontMatter>(
    //             new StringReader(yaml)
    //         ) ?? throw new FormatException("Error parsing front matter");
    //     var section = SiteHelper.GetSection(fileRelativePath);
    //     frontMatter.RawContent = rawContent;
    //     frontMatter.Section = section;
    //     frontMatter.SourceRelativePath = fileRelativePath;
    //     frontMatter.SourceFullPath = fileFullPath;
    //     frontMatter.Type ??= section;
    //     return frontMatter;
    // }

    /// <inheritdoc/>
    public T Parse<T>(string content)
    {
        try
        {
            return deserializer.Deserialize<T>(content);
        }
        catch
        {
            throw new FormatException("Error parsing front matter");
        }
    }

    /// <inheritdoc/>
    public void Export<T>(T data, string path)
    {
        var deserializer = new SerializerBuilder()
        .IgnoreFields()
        .ConfigureDefaultValuesHandling(
            DefaultValuesHandling.OmitEmptyCollections
            | DefaultValuesHandling.OmitDefaults
            | DefaultValuesHandling.OmitNull)
        .Build();
        var dataString = deserializer.Serialize(data);
        File.WriteAllText(path, dataString);
    }

    /// <inheritdoc/>
    public (string, string) SplitFrontMatter(in string fileContent)
    {
        using var content = new StringReader(fileContent);
        var frontMatterBuilder = new StringBuilder();
        string? line;

        while ((line = content.ReadLine()) != null && line != "---") { }
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
