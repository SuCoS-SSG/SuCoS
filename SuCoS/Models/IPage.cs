using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Markdig;
using SuCoS.Helpers;

namespace SuCoS.Models;

/// <summary>
/// Each page data created from source files or from the system.
/// </summary>
public interface IPage : IOutput, IFrontMatter, IFile
{
    /// <summary>
    /// The underlining content source
    /// </summary>
    ContentSource ContentSource { get; }

    /// <summary>
    /// The source directory of the file.
    /// </summary>
    string? SourcePathLastDirectory => string.IsNullOrEmpty(ContentSource.SourceRelativePathDirectory)
    ? null
    : Path.GetFileName(Path.GetFullPath(ContentSource.SourceRelativePathDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

    /// <summary>
    /// Point to the site configuration.
    /// </summary>
    ISite Site { get; }

    /// <summary>
    /// Secondary URL patterns to be used to create the url.
    /// </summary>
    Collection<string>? AliasesProcessed { get; }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    ConcurrentBag<string> PagesReferences { get; }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    IPage? Parent { get; }

    /// <summary>
    /// Plain markdown content, without HTML.
    /// </summary>
    string Plain => Markdown.ToPlainText(ContentSource.RawContent, SiteHelper.MarkdownPipeline);

    /// <summary>
    /// A list of tags, if any.
    /// </summary>
    List<IPage> TagsReference { get; }

    /// <summary>
    /// Just a simple check if the current page is the home page
    /// </summary>
    bool IsHome => Site.Home == this;

    /// <summary>
    /// Just a simple check if the current page is a "page"
    /// </summary>
    bool IsPage => (Kind & Kind.single) == Kind.single && (Kind & Kind.system) != Kind.system;

    /// <summary>
    /// Just a simple check if the current page is a section page
    /// </summary>
    bool IsSection => Type == "section";

    /// <summary>
    /// The number of words in the main content
    /// </summary>
    int WordCount => Plain.Split(NonWords, StringSplitOptions.RemoveEmptyEntries).Length;

    protected static readonly char[] NonWords = [' ', ',', ';', '.', '!', '"', '(', ')', '?', '\n', '\r'];

    /// <summary>
    /// The kind of the page, if it's a single page, a list of pages or the home page.
    /// It's used to determine the proper theme file.
    /// </summary>
    Kind Kind { get; }

    /// <summary>
    /// The markdown content converted to HTML
    /// </summary>
    string ContentPreRendered { get; }

    /// <summary>
    /// The processed content.
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Creates the output file by applying the theme templates to the page content.
    /// </summary>
    /// <returns>The processed output file content.</returns>
    string CompleteContent { get; }

    /// <summary>
    /// The output format used
    /// </summary>
    public string OutputFormat { get; set; }

    /// <summary>
    /// All output formats of the content has
    /// </summary>
    public List<string> OutputFormats { get; }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    IEnumerable<IPage> Pages { get; }

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    IEnumerable<IPage> RegularPages { get; }

    /// <summary>
    /// Get all URLs related to this content.
    /// </summary>
    Dictionary<string, IOutput> AllOutputUrLs { get; }

    /// <summary>
    /// Gets the Permalink path for the file.
    /// </summary>
    /// <param name="urlForce">The URL to consider. If null use the predefined URL</param>
    /// <returns>The output path.</returns>
    string CreatePermalink(string? urlForce = null);

    /// <summary>
    /// Final steps of parsing the content.
    /// </summary>
    void PostProcess();
}
