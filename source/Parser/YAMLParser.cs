using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SuCoS.Helpers;
using SuCoS.Models;
using YamlDotNet.Serialization;

namespace SuCoS.Parser;

/// <summary>
/// Responsible for parsing the content front matter using YAML
/// </summary>
public class YAMLParser : IFrontMatterParser
{
    /// <summary>
    /// YamlDotNet parser, strictly set to allow automatically parse only known fields
    /// </summary>
    private readonly IDeserializer yamlDeserializerRigid = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// YamlDotNet parser to loosely parse the YAML file. Used to include all non-matching fields
    /// into Params.
    /// </summary>
    private readonly IDeserializer yamlDeserializer = new DeserializerBuilder()
        .Build();

    /// <inheritdoc/>
    public FrontMatter ParseFrontmatterAndMarkdownFromFile( in string filePath, in string? sourceContentPath = null)
    {
        if (filePath is null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        string? fileContent;
        string? fileRelativePath;
        try
        {
            fileContent = File.ReadAllText(filePath);
            fileRelativePath = Path.GetRelativePath(sourceContentPath ?? string.Empty, filePath);
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException(filePath, ex);
        }

        return ParseFrontmatterAndMarkdown(fileRelativePath, fileContent);
    }

    /// <inheritdoc/>
    public FrontMatter ParseFrontmatterAndMarkdown(in string fileRelativePath, in string fileContent)
    {
        if (fileRelativePath is null)
        {
            throw new ArgumentNullException(nameof(fileRelativePath));
        }

        using var content = new StringReader(fileContent);
        var frontMatterBuilder = new StringBuilder();
        string? line;

        while ((line = content.ReadLine()) != null && line != "---") { }
        while ((line = content.ReadLine()) != null && line != "---")
        {
            frontMatterBuilder.AppendLine(line);
        }

        // Join the read lines to form the front matter
        var yaml = frontMatterBuilder.ToString();
        var rawContent = content.ReadToEnd();

        // Now, you can parse the YAML front matter
        var page = ParseYAML(fileRelativePath, yaml, rawContent);

        return page;
    }

    private FrontMatter ParseYAML(in string filePath, string yaml, in string rawContent)
    {
        var frontMatter = yamlDeserializerRigid.Deserialize<FrontMatter>(new StringReader(yaml)) ?? throw new FormatException("Error parsing front matter");
        var section = SiteHelper.GetSection(filePath);
        frontMatter.RawContent = rawContent;
        frontMatter.Section = section;
        frontMatter.SourcePath = filePath;
        frontMatter.Type ??= section;

        var yamlObject = yamlDeserializer.Deserialize(new StringReader(yaml));
        if (yamlObject is not Dictionary<object, object> yamlDictionary)
        {
            return frontMatter;
        }
        ParseParams(frontMatter, typeof(Page), yaml, yamlDictionary);

        return frontMatter;
    }

    /// <inheritdoc/>
    public SiteSettings ParseSiteSettings(string yaml)
    {
        var settings = yamlDeserializerRigid.Deserialize<SiteSettings>(yaml);
        ParseParams(settings, typeof(SiteSettings), yaml);
        return settings;
    }

    /// <summary>
    ///  Parse all YAML files for non-matching fields.
    /// </summary>
    /// <param name="settings">Site or Frontmatter object, that implements IParams</param>
    /// <param name="type">The type (Site or Frontmatter)</param>
    /// <param name="yaml">YAML content</param>
    /// <param name="yamlObject">yamlObject already parsed if available</param>
    public void ParseParams(IParams settings, Type type, string yaml, object? yamlObject = null)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        yamlObject ??= yamlDeserializer.Deserialize(new StringReader(yaml));
        if (yamlObject is not Dictionary<object, object> yamlDictionary)
        {
            return;
        }

        foreach (var key in yamlDictionary.Keys.Cast<string>())
        {
            // If the property is not a standard Frontmatter property
            if (type.GetProperty(key) != null)
            {
                continue;
            }

            // Recursively create a dictionary structure for the value
            if (yamlDictionary[key] is Dictionary<object, object> valueDictionary)
            {
                settings.Params[key] = valueDictionary;
            }
            else
            {
                settings.Params[key] = yamlDictionary[key];
            }
        }
    }
}
