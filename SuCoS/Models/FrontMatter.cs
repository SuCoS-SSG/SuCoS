using SuCoS.Helpers;
using SuCoS.Parsers;
using YamlDotNet.Serialization;

namespace SuCoS.Models;

/// <summary>
/// A scaffold structure to help creating system-generated content, like
/// tag, section or index pages
/// </summary>
[YamlSerializable]
public class FrontMatter : IFrontMatter
{
    #region IFrontMatter

    /// <inheritdoc/>
    public string? Title { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Type { get; set; } = "page";

    /// <inheritdoc/>
    public string? Url { get; set; }

    /// <inheritdoc/>
    public bool? Draft { get; set; }

    /// <inheritdoc/>
    public List<string>? Aliases { get; set; }

    /// <inheritdoc/>
    public string? Section { get; set; } = string.Empty;

    /// <inheritdoc/>
    public DateTime? Date { get; set; }

    /// <inheritdoc/>
    public DateTime? LastMod { get; set; }

    /// <inheritdoc/>
    public DateTime? PublishDate { get; set; }

    /// <inheritdoc/>
    public DateTime? ExpiryDate { get; set; }

    /// <inheritdoc/>
    public int Weight { get; set; }

    /// <inheritdoc/>
    public List<string>? Tags { get; set; }

    /// <inheritdoc/>
    public List<FrontMatterResources>? ResourceDefinitions { get; set; }

    /// <inheritdoc/>
    [YamlIgnore]
    public string RawContent { get; set; } = string.Empty;

    /// <inheritdoc/>
    [YamlIgnore]
    public string? SourceRelativePath { get; set; }

    /// <inheritdoc/>
    [YamlIgnore]
    public string SourceFullPath { get; set; }

    /// <inheritdoc/>
    [YamlIgnore]
    public string SourceRelativePathDirectory =>
        (Path.GetDirectoryName(SourceRelativePath) ?? string.Empty)
        .Replace('\\', '/');

    /// <inheritdoc/>
    [YamlIgnore]
    public string SourceFileNameWithoutExtension =>
        (Path.GetFileNameWithoutExtension(SourceRelativePath) ?? string.Empty)
        .Replace('\\', '/');

    /// <inheritdoc/>
    [YamlIgnore]
    public DateTime? GetPublishDate => PublishDate ?? Date;

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = [];

    #endregion IFrontMatter

    /// <summary>
    /// Cascade front matter data to its children.
    /// </summary>
    public FrontMatter? Cascade { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public FrontMatter()
    {
        SourceFullPath = string.Empty;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="title"></param>
    /// <param name="sourcePath"></param>
    public FrontMatter(string title, string sourcePath)
    {
        Title = title;
        SourceRelativePath = sourcePath;
        SourceFullPath = sourcePath;
    }

    /// <summary>
    /// Create a front matter from a given metadata + content
    /// </summary>
    /// <param name="metadata"></param>
    /// <param name="rawContent"></param>
    /// <param name="fileFullPath"></param>
    /// <param name="fileRelativePath"></param>
    /// <param name="parser"></param>
    /// <returns></returns>
    public static FrontMatter Parse(
        string metadata,
        in string rawContent,
        in string fileFullPath,
        in string fileRelativePath,
        IMetadataParser parser
    )
    {
        ArgumentNullException.ThrowIfNull(fileFullPath);
        ArgumentNullException.ThrowIfNull(fileRelativePath);
        ArgumentNullException.ThrowIfNull(parser);

        var frontMatter = parser.Parse<FrontMatter>(metadata);
        var section = SiteHelper.GetSection(fileRelativePath);
        frontMatter.RawContent = rawContent;
        frontMatter.Section = section;
        frontMatter.SourceRelativePath = fileRelativePath;
        frontMatter.SourceFullPath = fileFullPath;
        frontMatter.Type ??= section;
        return frontMatter;
    }

    /// <summary>
    /// Create a front matter from a given content
    /// </summary>
    /// <param name="fileFullPath"></param>
    /// <param name="fileRelativePath"></param>
    /// <param name="parser"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static FrontMatter Parse(
        in string fileFullPath,
        in string fileRelativePath,
        IMetadataParser parser,
        string content
    )
    {
        ArgumentNullException.ThrowIfNull(fileFullPath);
        ArgumentNullException.ThrowIfNull(fileRelativePath);
        ArgumentNullException.ThrowIfNull(parser);

        var (metadata, rawContent) = parser.SplitFrontMatter(content);
        return Parse(metadata, rawContent, fileFullPath, fileRelativePath,
            parser);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="other"></param>
    public FrontMatter Merge(FrontMatter other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return new FrontMatter
        {
            Title = string.IsNullOrEmpty(other.Title) ? Title : other.Title,
            Type = string.IsNullOrEmpty(other.Type) || other.Type == "page" ? Type : other.Type,
            Url = string.IsNullOrEmpty(other.Url) ? Url : other.Url,
            Draft = other.Draft ?? Draft,
            Aliases = other.Aliases ?? Aliases,
            Section = string.IsNullOrEmpty(other.Section) ? Section : other.Section,
            Date = other.Date ?? Date,
            LastMod = other.LastMod ?? LastMod,
            PublishDate = other.PublishDate ?? PublishDate,
            ExpiryDate = other.ExpiryDate ?? ExpiryDate,
            Weight = other.Weight != 0 ? other.Weight : Weight,
            Tags = other.Tags ?? Tags,
            ResourceDefinitions = other.ResourceDefinitions ?? ResourceDefinitions,
            RawContent = string.IsNullOrEmpty(other.RawContent) ? RawContent : other.RawContent,
            SourceRelativePath = string.IsNullOrEmpty(other.SourceRelativePath) ? SourceRelativePath : other.SourceRelativePath,
            SourceFullPath = string.IsNullOrEmpty(other.SourceFullPath) ? SourceFullPath : other.SourceFullPath,
            Params = other.Params.Any() ? other.Params : Params,
            Cascade = other.Cascade ?? Cascade
        };
    }

}
