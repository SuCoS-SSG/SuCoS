using System.Collections.Concurrent;
using Serilog;
using SuCoS.Helpers;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parsers;
using SuCoS.TemplateEngine;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, primarily extracted from the app.yaml file.
/// </summary>
public interface ISite : ISiteSettings
{
    /// <summary>
    /// SuCos internal variables
    /// </summary>
    public Sucos SuCoS { get; }

    /// <summary>
    /// Command line options
    /// </summary>
    public IGenerateOptions Options { get; set; }

    /// <summary>
    /// Theme used.
    /// </summary>
    public Theme? Theme { get; }

    /// <summary>
    /// The path of the content, based on the source path.
    /// </summary>
    public string SourceContentPath { get; }

    /// <summary>
    /// The path of the static content (that will be copied as is), based on the source path.
    /// </summary>
    public string SourceStaticPath { get; }

    /// <summary>
    /// The path theme.
    /// </summary>
    public string SourceThemePath { get; }

    /// <summary>
    /// List of all pages, including generated, by their permalink.
    /// </summary>
    public ConcurrentDictionary<string, IOutput> OutputReferences { get; }

    /// <summary>
    /// List of all pages, including generated.
    /// </summary>
    public IEnumerable<IPage> Pages { get; }

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public IEnumerable<IPage> RegularPages { get; }

    /// <summary>
    /// The page of the home page;
    /// </summary>
    public IPage? Home { get; }

    /// <summary>
    /// Manage all caching lists for the site
    /// </summary>
    public SiteCacheManager CacheManager { get; }

    /// <summary>
    /// Fronrt Matter parser
    /// </summary>
    public IFrontMatterParser Parser { get; }

    /// <summary>
    /// The template engine.
    /// </summary>
    public ITemplateEngine TemplateEngine { get; }

    /// <summary>
    /// The logger instance.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// List of all basic source folders
    /// </summary>
    public IEnumerable<string> SourceFolders { get; }

    /// <summary>
    /// Resets the template cache to force a reload of all templates.
    /// </summary>
    public void ResetCache();

    /// <summary>
    /// Search recursively for all markdown files in the content folder, then
    /// parse their content for front matter front matter and markdown.
    /// </summary>
    /// <param name="fs"></param>
    /// <param name="directory">Folder to scan</param>
    /// <param name="level">Folder recursive level</param>
    /// <param name="parent">Page of the upper directory</param>
    /// <param name="cascade"></param>
    /// <returns></returns>
    public void ScanAndParseSourceFiles(
        IFileSystem fs,
        string? directory = null,
        int level = 0,
        ContentSource? parent = null,
        FrontMatter? cascade = null);

    /// <summary>
    /// Expand the front matter to full blown pages.
    /// </summary>
    public void ProcessPages();

    /// <summary>
    /// Extra calculation and automatic data for each page.
    /// </summary>
    /// <param name="page">The given page to be processed</param>
    /// <param name="overwrite"></param>
    public void PostProcessPage(
        in IPage page,
        bool overwrite = false);

    /// <summary>
    /// Check if the page have the conditions to be published: valid date and not draft,
    /// unless a command line option to force it.
    /// </summary>
    /// <param name="frontMatter">Page or front matter</param>
    /// <param name="options">options</param>
    /// <returns></returns>
    public bool IsPageValid(
        in IContentSource frontMatter,
        IGenerateOptions? options);

    /// <summary>
    /// Check if the page have a publishing date from the past.
    /// </summary>
    /// <param name="contentSource">Page or content Source</param>
    /// <param name="options">options</param>
    /// <returns></returns>
    public bool IsDateValid(
        in IContentSource contentSource,
        IGenerateOptions? options);

    /// <summary>
    /// Check if the page is expired
    /// </summary>
    public bool IsDateExpired(in IContentSource contentSource);

    /// <summary>
    /// Check if the page is publishable
    /// </summary>
    public bool IsDatePublishable(in IContentSource contentSource);

    /// <summary>
    /// Create a Page from front matter
    /// </summary>
    /// <param name="contentSource"></param>
    public List<Page> PageCreate(ContentSource contentSource);

    /// <summary>
    /// Include the Front Matter into the site
    /// </summary>
    /// <param name="contentSource"></param>
    ContentSource? ContentSourceAdd(ContentSource? contentSource);
}
