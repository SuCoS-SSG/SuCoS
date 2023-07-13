using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Fluid;
using Serilog;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, primarily extracted from the app.yaml file.
/// </summary>
public interface ISite : IParams
{
    /// <summary>
    /// Command line options
    /// </summary>
    public IGenerateOptions Options { get; set; }

    #region SiteSettings

    /// <summary>
    /// Site Title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    public bool UglyURLs { get; }

    #endregion SiteSettings

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
    /// The path of the static content (that will be copied as is), based on the theme path.
    /// </summary>
    public string SourceThemeStaticPath => Path.Combine(SourceThemePath, "static");

    /// <summary>
    /// List of all pages, including generated.
    /// </summary>
    public IEnumerable<IPage> Pages { get; }

    /// <summary>
    /// List of all pages, including generated, by their permalink.
    /// </summary>
    public ConcurrentDictionary<string, IPage> PagesReferences { get; }

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public List<IPage> RegularPages { get; }

    /// <summary>
    /// The page of the home page;
    /// </summary>
    public IPage? Home { get; }

    /// <summary>
    /// Manage all caching lists for the site
    /// </summary>
    public SiteCacheManager CacheManager { get; }

    /// <summary>
    /// The Fluid parser instance.
    /// </summary>
    public FluidParser FluidParser { get; }

    /// <summary>
    /// The Fluid/Liquid template options.
    /// </summary>
    public TemplateOptions TemplateOptions { get; }
    /// <summary>
    /// The logger instance.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Resets the template cache to force a reload of all templates.
    /// </summary>
    public void ResetCache();

    /// <summary>
    /// Search recursively for all markdown files in the content folder, then
    /// parse their content for front matter meta data and markdown.
    /// </summary>
    /// <param name="directory">Folder to scan</param>
    /// <param name="level">Folder recursive level</param>
    /// <param name="parent">Page of the upper directory</param>
    /// <returns></returns>
    public void ParseAndScanSourceFiles(string? directory, int level = 0, IPage? parent = null);

    /// <summary>
    /// Extra calculation and automatic data for each page.
    /// </summary>
    /// <param name="page">The given page to be processed</param>
    /// <param name="parent">The parent page, if any</param>
    /// <param name="overwrite"></param>
    public void PostProcessPage(in IPage page, IPage? parent = null, bool overwrite = false);

    /// <summary>
    /// Check if the page have a publishing date from the past.
    /// </summary>
    /// <param name="frontMatter">Page or front matter</param>
    /// <param name="options">options</param>
    /// <returns></returns>
    public bool IsValidDate(in IFrontMatter frontMatter, IGenerateOptions? options);

    /// <summary>
    /// Check if the page is expired
    /// </summary>
    public bool IsDateExpired(in IFrontMatter frontMatter);

    /// <summary>
    /// Check if the page is publishable
    /// </summary>
    public bool IsDatePublishable(in IFrontMatter frontMatter);
}