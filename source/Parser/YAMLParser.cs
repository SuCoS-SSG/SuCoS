using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SuCoS.Helper;
using SuCoS.Models;
using YamlDotNet.Serialization;

namespace SuCoS.Parser;

/// <summary>
/// Responsible for parsing the content frontmatter using YAML
/// </summary>
public partial class YAMLParser : IFrontmatterParser
{
    [GeneratedRegex(@"^---\s*[\r\n](?<frontmatter>.*?)[\r\n]---\s*", RegexOptions.Singleline)]
    private partial Regex YAMLRegex();

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
    public Frontmatter? ParseFrontmatter(Site site, string filePath, ref string fileContent)
    {
        if (site is null)
        {
            throw new ArgumentNullException(nameof(site));
        }
        if (filePath is null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        var match = YAMLRegex().Match(fileContent);
        if (match.Success)
        {
            var yaml = match.Groups["frontmatter"].Value;
            fileContent = fileContent[match.Length..].TrimStart('\n');
            var frontmatter = ParseYAML(filePath, site, yaml, fileContent);
            return frontmatter;
        }
        return null;
    }

    private Frontmatter ParseYAML(string filePath, Site site, string frontmatter, string fileContent)
    {
        var page = yamlDeserializerRigid.Deserialize<Frontmatter>(frontmatter);
        var sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
        var section = SiteHelper.GetSection(filePath);
        page.RawContent = fileContent;
        page.Section = section;
        page.Site = site;
        page.SourceFileNameWithoutExtension = sourceFileNameWithoutExtension;
        page.SourcePath = filePath;
        page.SourcePathDirectory = Path.GetDirectoryName(filePath);
        page.Title ??= sourceFileNameWithoutExtension;
        page.Type ??= section;

        var yamlObject = yamlDeserializer.Deserialize(new StringReader(frontmatter));
        if (yamlObject is Dictionary<object, object> yamlDictionary)
        {
            if (yamlDictionary.TryGetValue("Tags", out var tags) && tags is List<string> tagsValues)
            {
                foreach (var tagName in tagsValues)
                {
                    var contentTemplate = new BasicContent(
                        title: tagName,
                        section: "tags",
                        type: "tags",
                        url: "tags/" + Urlizer.Urlize(tagName)
                    );
                    _ = site.CreateAutomaticFrontmatter(contentTemplate, page);
                }
            }
            ParseParams(page, typeof(Frontmatter), frontmatter, yamlDictionary);
        }
        return page;
    }

    /// <inheritdoc/>
    public Site ParseSiteSettings(string content)
    {
        var settings = yamlDeserializerRigid.Deserialize<Site>(content);
        ParseParams(settings, typeof(Site), content);
        return settings;
    }

    /// <summary>
    ///  Parse all YAML files for non-matching fields.
    /// </summary>
    /// <param name="settings">Site or Frontmatter object, that implements IParams</param>
    /// <param name="type">The type (Site or Frontmatter)</param>
    /// <param name="content">YAML content</param>
    /// <param name="yamlObject">yamlObject already parsed if available</param>
    public void ParseParams(IParams settings, Type type, string content, object? yamlObject = null)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        yamlObject ??= yamlDeserializer.Deserialize(new StringReader(content));
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
