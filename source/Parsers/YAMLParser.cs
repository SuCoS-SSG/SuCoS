using System.Text;
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

    /// <inheritdoc/>
    public IFrontMatter ParseFrontmatterAndMarkdownFromFile(
        in string fileFullPath,
        in string? sourceContentPath = null
    )
    {
        ArgumentNullException.ThrowIfNull(fileFullPath);

        string? fileContent;
        string? fileRelativePath;
        try
        {
            fileContent = File.ReadAllText(fileFullPath);
            fileRelativePath = Path.GetRelativePath(
                sourceContentPath ?? string.Empty,
                fileFullPath
            );
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException(fileFullPath, ex);
        }

        return ParseFrontmatterAndMarkdown(
            fileFullPath,
            fileRelativePath,
            fileContent
        );
    }

    /// <inheritdoc/>
    public IFrontMatter ParseFrontmatterAndMarkdown(
        in string fileFullPath,
        in string fileRelativePath,
        in string fileContent
    )
    {
        ArgumentNullException.ThrowIfNull(fileRelativePath);

        using var content = new StringReader(fileContent);
        var frontMatterBuilder = new StringBuilder();
        string? line;

        while ((line = content.ReadLine()) != null && line != "---") { }
        while ((line = content.ReadLine()) != null && line != "---")
        {
            _ = frontMatterBuilder.AppendLine(line);
        }

        // Join the read lines to form the front matter
        var yaml = frontMatterBuilder.ToString();
        var rawContent = content.ReadToEnd();

        // Now, you can parse the YAML front matter
        var page = ParseYAML(fileFullPath, fileRelativePath, yaml, rawContent);

        return page;
    }

    private FrontMatter ParseYAML(
        in string fileFullPath,
        in string fileRelativePath,
        string yaml,
        in string rawContent
    )
    {
        var frontMatter =
            deserializer.Deserialize<FrontMatter>(
                new StringReader(yaml)
            ) ?? throw new FormatException("Error parsing front matter");
        var section = SiteHelper.GetSection(fileRelativePath);
        frontMatter.RawContent = rawContent;
        frontMatter.Section = section;
        frontMatter.SourceRelativePath = fileRelativePath;
        frontMatter.SourceFullPath = fileFullPath;
        frontMatter.Type ??= section;
        return frontMatter;
    }

    /// <inheritdoc/>
    public T Parse<T>(string content)
    {
        var data = deserializer.Deserialize<T>(content);
        return data;
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
}
