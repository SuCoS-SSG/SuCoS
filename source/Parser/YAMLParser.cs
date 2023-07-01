using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SuCoS.Helper;
using SuCoS.Models;
using YamlDotNet.Serialization;

namespace SuCoS.Parser;

/// <summary>
/// Responsible for parsing the content frontmatter using YAML
/// </summary>
public partial class YAMLParser : IFrontmatterParser
{
    /// <summary>
    /// YamlDotNet parser, strictly set to allow automatically parse only known fields
    /// </summary>
    readonly IDeserializer yamlDeserializerRigid = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// YamlDotNet parser to loosely parse the YAML file. Used to include all non-matching fields
    /// into Params.
    /// </summary>
    readonly IDeserializer yamlDeserializer = new DeserializerBuilder()
        .Build();

    /// <inheritdoc/>
    public Frontmatter? ParseFrontmatterAndMarkdownFromFile(Site site, in string filePath, in string? sourceContentPath = null)
    {
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }
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

        return ParseFrontmatterAndMarkdown(site, fileRelativePath, fileContent);
    }

    /// <inheritdoc/>
    public Frontmatter? ParseFrontmatterAndMarkdown(Site site, in string fileRelativePath, in string fileContent)
    {
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }
        if (fileRelativePath is null)
        {
            throw new ArgumentNullException(nameof(fileRelativePath));
        }

        using var content = new StringReader(fileContent);
        var frontmatterBuilder = new StringBuilder();
        string? line;

        while ((line = content.ReadLine()) != null && line != "---") { }
        while ((line = content.ReadLine()) != null && line != "---")
        {
            frontmatterBuilder.AppendLine(line);
        }

        // Join the read lines to form the frontmatter
        var yaml = frontmatterBuilder.ToString();
        var rawContent = content.ReadToEnd();

        // Now, you can parse the YAML frontmatter
        var page = ParseYAML(ref site, fileRelativePath, yaml, rawContent);

        return page;
    }

    private Frontmatter ParseYAML(ref Site site, in string filePath, string yaml, in string rawContent)
    {
        var page = yamlDeserializerRigid.Deserialize<Frontmatter>(new StringReader(yaml)) ?? throw new FormatException("Error parsing frontmatter");
        var sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
        var section = SiteHelper.GetSection(filePath);
        page.RawContent = rawContent;
        page.Section = section;
        page.Site = site;
        page.SourceFileNameWithoutExtension = sourceFileNameWithoutExtension;
        page.SourcePath = filePath;
        page.SourcePathDirectory = Path.GetDirectoryName(filePath);
        page.Title ??= sourceFileNameWithoutExtension;
        page.Type ??= section;

        var yamlObject = yamlDeserializer.Deserialize(new StringReader(yaml));
        if (yamlObject is Dictionary<object, object> yamlDictionary)
        {
            if (yamlDictionary.TryGetValue("Tags", out var tags) && tags is List<object> tagsValues)
            {
                foreach (var tagObj in tagsValues)
                {
                    var tagName = (string)tagObj;
                    var contentTemplate = new BasicContent(
                        title: tagName,
                        section: "tags",
                        type: "tags",
                        url: "tags/" + Urlizer.Urlize(tagName)
                    );
                    _ = site.CreateAutomaticFrontmatter(contentTemplate, page);
                }
            }
            ParseParams(page, typeof(Frontmatter), yaml, yamlDictionary);
        }
        return page;
    }

    /// <inheritdoc/>
    public Site ParseSiteSettings(string yaml)
    {
        var settings = yamlDeserializerRigid.Deserialize<Site>(yaml);
        ParseParams(settings, typeof(Site), yaml);
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
        if (yamlObject is Dictionary<object, object> yamlDictionary)
        {
            foreach (var key in yamlDictionary.Keys.Cast<string>())
            {
                // If the property is not a standard Frontmatter property
                if (type.GetProperty(key) == null)
                {
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
    }
}
