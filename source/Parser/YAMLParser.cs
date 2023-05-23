using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SuCoS;

/// <summary>
/// Responsible for parsing the content frontmatter using YAML
/// </summary>
public class YAMLParser : IFrontmatterParser
{
    /// <inheritdoc/>
    public Frontmatter ParseFrontmatter(ref string fileContent)
    {
        Frontmatter frontmatter = new();
        var match = Regex.Match(fileContent, @"^---\s*\n(?<frontmatter>.*?)\n---\s*\n", RegexOptions.Singleline);
        if (match.Success)
        {
            var frontmatterString = match.Groups["frontmatter"].Value;
            fileContent = fileContent[match.Length..].TrimStart('\n');

            // Parse the front matter string into Frontmatter properties
            var yamlDeserializer = new DeserializerBuilder().Build();
            var yamlObject = yamlDeserializer.Deserialize(new StringReader(frontmatterString));

            if (yamlObject is Dictionary<object, object> frontmatterDictionary)
            {
                _ = (frontmatterDictionary.TryGetValue("Title", out var titleValue) && titleValue != null).ToString();
                _ = (frontmatterDictionary.TryGetValue("URL", out var urlValue) && urlValue != null).ToString();
                List<string> tags = new();
                if (frontmatterDictionary.TryGetValue("Tags", out var tagsValue) && tagsValue is List<object> tagsList)
                {
                    tags = tagsList.Select(tag => tag.ToString()!).ToList();
                }

                frontmatter = new Frontmatter
                {
                    Title = titleValue?.ToString(),
                    URL = urlValue?.ToString(),
                    Tags = tags,
                    ContentRaw = fileContent
                };
            }
        }
        else
        {
            frontmatter.ContentRaw = fileContent;
        }
        return frontmatter;
    }

    /// <inheritdoc/>
    public AppConfig ParseAppConfig(string configFileContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var config = deserializer.Deserialize<AppConfig>(configFileContent);
        return config;
    }
}