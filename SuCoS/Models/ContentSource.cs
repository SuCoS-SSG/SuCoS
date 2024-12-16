using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace SuCoS.Models;

/// <summary>
/// Content Source = front matter + raw content
/// </summary>
public class ContentSource : IContentSource, IFile
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
    public string RawContent { get; set; }

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
    /// List of children content.
    /// </summary>
    public HashSet<ContentSource> Children { get; set; } = [];

    /// <summary>
    /// ctr
    /// </summary>
    /// <param name="sourceRelativePath"></param>
    /// <param name="frontMatter"></param>
    /// <param name="rawContent"></param>
    public ContentSource(string sourceRelativePath, FrontMatter frontMatter, string rawContent)
    {
        SourceRelativePath = sourceRelativePath;
        FrontMatter = frontMatter;
        RawContent = rawContent;
    }

    /// <summary>
    /// List of attached resources
    /// </summary>
    public Collection<ContentSourceResource>? RawResources { get; private set; }

    /// <summary>
    /// Scan and collect resources for this content source
    /// </summary>
    public ContentSource ScanForResources([NotNull] ISite site)
    {
        if (BundleType == BundleType.none)
        {
            return this;
        }

        var sourceFullDir = (this as IFile)
            .SourceFullPathDirectory(site.SourceContentPath);
        var sourceFullPath = (this as IFile)
            .SourceFullPath(site.SourceContentPath);

        if (string.IsNullOrEmpty(sourceFullDir) || !Directory.Exists(sourceFullDir))
        {
            return this;
        }

        var resourceFiles = Directory.GetFiles(sourceFullDir)
            .Where(file =>
                file != sourceFullPath &&
                (BundleType == BundleType.leaf ||
                 !file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            )
            .Select(file => Path.GetRelativePath(site.SourceContentPath, file));

        foreach (var resourceRelativePath in resourceFiles)
        {
            RawResources ??= [];
            var resource = new ContentSourceResource
            {
                SourceRelativePath = resourceRelativePath
            };
            RawResources.Add(resource);
        }

        return this;
    }
}
