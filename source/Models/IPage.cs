using Markdig;
using SuCoS.Helpers;
using System.Collections.Concurrent;

namespace SuCoS.Models;

/// <summary>
/// Each page data created from source files or from the system.
/// </summary>
public interface IPage : IFrontMatter, IOutput
{
    /// <inheritdoc/>
    new Kind Kind { get; set; }

    /// <summary>
    /// The source directory of the file.
    /// </summary>
    public string? SourcePathLastDirectory => string.IsNullOrEmpty(SourceRelativePathDirectory)
    ? null
    : Path.GetFileName(Path.GetFullPath(SourceRelativePathDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

    /// <summary>
    /// Point to the site configuration.
    /// </summary>
    public ISite Site { get; }

    /// <summary>
    /// Secondary URL patterns to be used to create the url.
    /// </summary>
    public List<string>? AliasesProcessed { get; set; }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    public ConcurrentBag<string> PagesReferences { get; }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    public IPage? Parent { get; set; }

    /// <summary>
    /// The bundle type of the page.
    /// </summary>
    public BundleType BundleType { get; set; }

    /// <summary>
    /// Page resources. All files that accompany a page.
    /// </summary>
    public List<Resource>? Resources { get; set; }

    /// <summary>
    /// Plain markdown content, without HTML.
    /// </summary>
    public string Plain => Markdown.ToPlainText(RawContent, SiteHelper.MarkdownPipeline);

    /// <summary>
    /// A list of tags, if any.
    /// </summary>
    public ConcurrentBag<IPage> TagsReference { get; }

    /// <summary>
    /// Just a simple check if the current page is the home page
    /// </summary>
    public bool IsHome => Site.Home == this;

    /// <summary>
    /// Just a simple check if the current page is a section page
    /// </summary>
    public bool IsSection => Type == "section";

    /// <summary>
    /// Just a simple check if the current page is a "page"
    /// </summary>
    public bool IsPage => Kind == Kind.single;

    /// <summary>
    /// The number of words in the main content
    /// </summary>
    public int WordCount => Plain.Split(nonWords, StringSplitOptions.RemoveEmptyEntries).Length;

    private static readonly char[] nonWords = { ' ', ',', ';', '.', '!', '"', '(', ')', '?', '\n', '\r' };

    /// <summary>
    /// The markdown content converted to HTML
    /// </summary>
    public string ContentPreRendered { get; }

    /// <summary>
    /// The processed content.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Creates the output file by applying the theme templates to the page content.
    /// </summary>
    /// <returns>The processed output file content.</returns>
    public string CompleteContent { get; }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    public IEnumerable<IPage> Pages { get; }

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public IEnumerable<IPage> RegularPages { get; }

    /// <summary>
    /// Get all URLs related to this content.
    /// </summary>
    public Dictionary<string, IOutput> AllOutputURLs { get; }

    /// <summary>
    /// Gets the Permalink path for the file.
    /// </summary>
    /// <param name="URLforce">The URL to consider. If null use the predefined URL</param>
    /// <returns>The output path.</returns>
    public string CreatePermalink(string? URLforce = null);

    /// <summary>
    /// Final steps of parsing the content.
    /// </summary>
    public void PostProcess();
}
