using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SuCoS.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        .WithNamingConvention(PascalCaseNamingConvention.Instance)
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

        Frontmatter? frontmatter = null;
        var match = YAMLRegex().Match(fileContent);
        if (match.Success)
        {
            var content = match.Groups["frontmatter"].Value;

            fileContent = fileContent[match.Length..].TrimStart('\n');

            // Parse the front matter string into Frontmatter properties
            var yamlObject = yamlDeserializer.Deserialize(new StringReader(content));

            if (yamlObject is Dictionary<object, object> frontmatterDictionary)
            {
                _ = frontmatterDictionary.TryGetValue("Title", out var titleValue);
                _ = frontmatterDictionary.TryGetValue("URL", out var urlValue);
                _ = frontmatterDictionary.TryGetValue("Type", out var typeValue);
                _ = frontmatterDictionary.TryGetValue("Date", out var dateValue);
                _ = frontmatterDictionary.TryGetValue("LastMod", out var dateLastModValue);
                _ = frontmatterDictionary.TryGetValue("PublishDate", out var datePublishValue);
                _ = frontmatterDictionary.TryGetValue("ExpiryDate", out var dateExpiryValue);
                var section = GetSection(filePath);

                List<string> tags = new();
                if (frontmatterDictionary.TryGetValue("Tags", out var tagsValue) && tagsValue is List<object> tagsListObj)
                {
                    foreach (var item in tagsListObj)
                    {
                        var value = item.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            tags.Add(value);
                        }
                    }
                }

                var sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
                frontmatter = new(
                    title: titleValue?.ToString() ?? sourceFileNameWithoutExtension,
                    site: site,
                    sourcePath: filePath,
                    sourceFileNameWithoutExtension: sourceFileNameWithoutExtension,
                    sourcePathDirectory: null
                )
                {
                    URL = urlValue?.ToString(),
                    Section = section,
                    Type = typeValue?.ToString() ?? section,
                    Kind = Kind.single,
                    Date = DateTime.TryParse(dateValue?.ToString(), out var parsedDate) ? parsedDate : null,
                    LastMod = DateTime.TryParse(dateLastModValue?.ToString(), out var parsedLastMod) ? parsedLastMod : null,
                    PublishDate = DateTime.TryParse(datePublishValue?.ToString(), out var parsedPublishDate) ? parsedPublishDate : null,
                    ExpiryDate = DateTime.TryParse(dateExpiryValue?.ToString(), out var parsedExpiryDate) ? parsedExpiryDate : null
                };

                if (frontmatterDictionary.TryGetValue("Aliases", out var aliasesValue) && aliasesValue is List<object> aliasesValueObj)
                {
                    frontmatter.Aliases ??= new List<string>();
                    foreach (var item in aliasesValueObj)
                    {
                        var value = item.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            frontmatter.Aliases.Add(value);
                        }
                    }
                }

                ParseParams(frontmatter, typeof(Frontmatter), content);

                foreach (var tagName in tags)
                {
                    var contentTemplate = new BasicContent(
                        title: tagName,
                        section: "tags",
                        type: "tags",
                        url: "tags/" + Urlizer.Urlize(tagName)
                    );
                    _ = site.CreateAutomaticFrontmatter(contentTemplate, frontmatter);
                }
            }
        }
        if (frontmatter is not null)
        {
            frontmatter.RawContent = fileContent;
            return frontmatter;
        }

        return null;
    }

    private static string GetSection(string filePath)
    {
        // Split the path into individual folders
        var folders = filePath?.Split(Path.DirectorySeparatorChar);

        // Retrieve the first folder
        return folders?[0] ?? string.Empty;
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
    void ParseParams(IParams settings, Type type, string content)
    {
        var yamlObject = yamlDeserializer.Deserialize(new StringReader(content));
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
