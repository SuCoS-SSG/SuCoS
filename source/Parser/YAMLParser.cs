using System;
using System.Collections.Generic;
using System.IO;
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
    private partial Regex _regex();

    /// <inheritdoc/>
    public Frontmatter? ParseFrontmatter(Site site, string filePath, ref string fileContent, ITaxonomyCreator taxonomyCreator)
    {
        if (site is null || taxonomyCreator is null)
        {
            throw new ArgumentNullException(nameof(site));
        }

        Frontmatter? frontmatter = null;
        var match = _regex().Match(fileContent);
        if (match.Success)
        {
            var frontmatterString = match.Groups["frontmatter"].Value;

            fileContent = fileContent[match.Length..].TrimStart('\n');

            // Parse the front matter string into Frontmatter properties
            var yamlDeserializer = new DeserializerBuilder().Build();
            var yamlObject = yamlDeserializer.Deserialize(new StringReader(frontmatterString));

            if (yamlObject is Dictionary<object, object> frontmatterDictionary)
            {
                _ = frontmatterDictionary.TryGetValue("Title", out var titleValue);
                _ = frontmatterDictionary.TryGetValue("URL", out var urlValue);
                _ = frontmatterDictionary.TryGetValue("Type", out var typeValue);
                var section = GetSection(site, filePath);

                List<string> tags = new();
                if (frontmatterDictionary.TryGetValue("Tags", out var tagsValue) && tagsValue is List<object> tagsListObj)
                {
                    foreach (var item in tagsListObj)
                    {
                        var value = item?.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            tags.Add(value);
                        }
                    }
                }

                var sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath) ?? string.Empty;
                frontmatter = new(
                    Title: titleValue?.ToString() ?? sourceFileNameWithoutExtension,
                    Site: site,
                    SourcePath: filePath,
                    SourceFileNameWithoutExtension: sourceFileNameWithoutExtension,
                    SourcePathDirectory: null
                )
                {
                    URL = urlValue?.ToString(),
                    Section = section,
                    Type = typeValue?.ToString() ?? section,
                    Kind = Kind.single,
                };

                foreach (var tagName in tags)
                {
                    _ = taxonomyCreator.CreateTagFrontmatter(site, tagName: tagName, frontmatter);
                }
            }
        }
        if (frontmatter is not null)
        {
            frontmatter.ContentRaw = fileContent;
            return frontmatter;
        }

        return null;
    }

    private static string GetSection(Site site, string filePath)
    {
        var relativePath = Path.GetRelativePath(site.SourceContentPath, filePath);

        // Get the directory name from the path
        var directoryName = Path.GetDirectoryName(relativePath);

        // Split the path into individual folders
        var folders = directoryName?.Split(Path.DirectorySeparatorChar);

        // Retrieve the first folder
        return folders?[0] ?? string.Empty;
    }

    /// <inheritdoc/>
    public Site ParseAppConfig(string configFileContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var config = deserializer.Deserialize<Site>(configFileContent);
        return config;
    }
}
