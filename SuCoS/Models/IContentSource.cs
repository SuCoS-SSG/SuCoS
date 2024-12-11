namespace SuCoS.Models;

/// <summary>
/// Content Source = front matter + raw content
/// </summary>
public interface IContentSource : IFrontMatter
{
    /// <summary>
    /// Raw content from the Markdown file, bellow the front matter.
    /// </summary>
    string RawContent { get; }

    /// <summary>
    /// The bundle type of the page.
    /// </summary>
    BundleType BundleType { get; }

    /// <summary>
    /// The kind of the page, if it's a single page, a list of pages or the home page.
    /// It's used to determine the proper theme file.
    /// </summary>
    Kind Kind { get; }

    /// <summary>
    /// Pages created based on this Content Source
    /// </summary>
    List<IPage> ContentSourceToPages { get; }

    /// <summary>
    /// List of tags.
    /// </summary>
    List<ContentSource> ContentSourceTags { get; }

    /// <summary>
    /// The Content Source parent content
    /// </summary>
    ContentSource? ContentSourceParent { get; }

    /// <summary>
    /// The date to be considered as the publishing date.
    /// </summary>
    DateTime? GetPublishDate => PublishDate ?? Date;
}
