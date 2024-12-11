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
    public Dictionary<string, object> Params { get; set; } = [];

    /// <summary>
    /// Cascade front matter data to its children.
    /// </summary>
    public FrontMatter? Cascade { get; set; }

    #endregion IFrontMatter

    /// <summary>
    /// Constructor
    /// </summary>
    public FrontMatter() { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="title"></param>
    /// <param name="sourcePath"></param>
    public FrontMatter(string title, string sourcePath)
    {
        Title = title;
    }

    /// <summary>
    /// Create a front matter from a given front matter + content
    /// </summary>
    /// <param name="frontMatterString"></param>
    /// <param name="fileFullPath"></param>
    /// <param name="fileRelativePath"></param>
    /// <param name="parser"></param>
    /// <returns></returns>
    public static FrontMatter Parse(
        string frontMatterString,
        in string fileFullPath,
        in string fileRelativePath,
        IFrontMatterParser parser
    )
    {
        ArgumentNullException.ThrowIfNull(fileFullPath);
        ArgumentNullException.ThrowIfNull(fileRelativePath);
        ArgumentNullException.ThrowIfNull(parser);

        var frontMatter = parser.Parse<FrontMatter>(frontMatterString);
        var section = SiteHelper.GetSection(fileRelativePath);
        frontMatter.Section = section;
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
    public static (FrontMatter, string) Parse(
        in string fileFullPath,
        in string fileRelativePath,
        IFrontMatterParser parser,
        string content
    )
    {
        ArgumentNullException.ThrowIfNull(fileFullPath);
        ArgumentNullException.ThrowIfNull(fileRelativePath);
        ArgumentNullException.ThrowIfNull(parser);

        var (frontMatter, rawContent) = parser.SplitFrontMatterAndContent(content);
        return (Parse(frontMatter, fileFullPath, fileRelativePath, parser), rawContent);
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
            Params = other.Params.Count != 0 ? other.Params : Params,
            Cascade = other.Cascade ?? Cascade
        };
    }
}
