namespace SuCoS.Models;

/// <summary>
/// Content Source = front matter + raw content
/// </summary>
public class ContentSource : IContentSource, IFrontMatter, IFile
{
    /// <summary>
    /// Internal front matter
    /// </summary>
    public FrontMatter FrontMatter { get; set; }

    #region IFrontMatter

    /// <inheritdoc />
    public string? Title => FrontMatter.Title;

    /// <inheritdoc />
    public string? Section => FrontMatter.Section;

    /// <inheritdoc />
    public string? Type
    {
        get => FrontMatter.Type;
        set => FrontMatter.Type = value;
    }

    /// <inheritdoc />
    public string? Url => FrontMatter.Url;

    /// <inheritdoc />
    public bool? Draft => FrontMatter.Draft;

    /// <inheritdoc />
    public DateTime? Date => FrontMatter.Date;

    /// <inheritdoc />
    public DateTime? LastMod => FrontMatter.LastMod;

    /// <inheritdoc />
    public DateTime? PublishDate => FrontMatter.PublishDate;

    /// <inheritdoc />
    public DateTime? ExpiryDate => FrontMatter.ExpiryDate;

    /// <inheritdoc />
    public List<string>? Aliases => FrontMatter.Aliases;

    /// <inheritdoc />
    public int Weight => FrontMatter.Weight;

    /// <inheritdoc />
    public List<string>? Tags => FrontMatter.Tags;

    /// <inheritdoc />
    public List<FrontMatterResources>? ResourceDefinitions
    {
        get => FrontMatter.ResourceDefinitions;
        set => FrontMatter.ResourceDefinitions = value;
    }

    #endregion IFrontMatter

    #region IContentSource

    /// <inheritdoc />
    public string RawContent { get; set; } = string.Empty;

    /// <inheritdoc />
    public BundleType BundleType { get; set; }

    /// <inheritdoc />
    public Kind Kind { get; set; } = Kind.single;

    /// <inheritdoc />
    public List<IPage> ContentSourceToPages { get; } = [];

    /// <inheritdoc />
    public ContentSource? ContentSourceParent { get; set; }

    /// <inheritdoc />
    public List<ContentSource> ContentSourceTags { get; } = [];

    #endregion IContentSource

    #region IParams
    /// <inheritdoc />
    public Dictionary<string, object> Params
    {
        get => FrontMatter.Params;
        set => FrontMatter.Params = value;
    }

    #endregion IParams

    #region IFile

    /// <inheritdoc />
    public string SourceRelativePath { get; set; }

    /// <inheritdoc />
    public string SourceRelativePathDirectory =>
        (Path.GetDirectoryName(SourceRelativePath) ?? string.Empty)
        .Replace('\\', '/');

    #endregion IFile

    /// <summary>
    /// List of tags.
    /// </summary>
    public HashSet<ContentSource> PagePages { get; set; } = [];

    /// <summary>
    /// ctr
    /// </summary>
    /// <param name="sourceRelativePath"></param>
    public ContentSource(string sourceRelativePath)
    {
        SourceRelativePath = sourceRelativePath;
        this.FrontMatter = new();
        this.RawContent = string.Empty;
    }

    /// <summary>
    /// ctr
    /// </summary>
    /// <param name="sourceRelativePath"></param>
    /// <param name="frontMatter"></param>
    public ContentSource(string sourceRelativePath, FrontMatter frontMatter)
    {
        SourceRelativePath = sourceRelativePath;
        this.FrontMatter = frontMatter;
    }

    /// <summary>
    /// ctr
    /// </summary>
    /// <param name="sourceRelativePath"></param>
    /// <param name="frontMatter"></param>
    /// <param name="rawContent"></param>
    public ContentSource(string sourceRelativePath, FrontMatter frontMatter, string rawContent)
    {
        SourceRelativePath = sourceRelativePath;
        this.FrontMatter = frontMatter;
        this.RawContent = rawContent;
    }
}
