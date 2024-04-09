using Fluid;
using Serilog;
using SuCoS.Helpers;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parser;
using System.Collections.Concurrent;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, primarily extracted from the app.yaml file.
/// </summary>
public class Site : ISite
{
    #region IParams

    /// <inheritdoc/>
    /// <inheritdoc/>
    public Dictionary<string, object> Params
    {
        get => settings.Params;
        set => settings.Params = value;
    }

    #endregion IParams

    /// <summary>
    /// Command line options
    /// </summary>
    public IGenerateOptions Options { get; set; }

    #region SiteSettings

    /// <summary>
    /// Site Title/Name
    /// </summary>
    public string Title => settings.Title;

    /// <summary>
    /// Site description
    /// </summary>
    public string? Description => settings.Description;

    /// <summary>
    /// Copyright information
    /// </summary>
    public string? Copyright => settings.Copyright;

    /// <summary>
    /// The base URL that will be used to build public links.
    /// </summary>
    public string BaseURL => settings.BaseURL;

    /// <summary>
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    public bool UglyURLs => settings.UglyURLs;

    #endregion SiteSettings

    /// <summary>
    /// The path of the content, based on the source path.
    /// </summary>
    public string SourceContentPath => Path.Combine(Options.Source, "content");

    /// <summary>
    /// The path of the static content (that will be copied as is), based on the source path.
    /// </summary>
    public string SourceStaticPath => Path.Combine(Options.Source, "static");

    /// <summary>
    /// The path theme.
    /// </summary>
    public string SourceThemePath => Path.Combine(Options.Source, "theme");

    /// <summary>
    /// The path of the static content (that will be copied as is), based on the theme path.
    /// </summary>
    public string SourceThemeStaticPath => Path.Combine(SourceThemePath, "static");

    /// <summary>
    /// List of all basic source folders
    /// </summary>
    public IEnumerable<string> SourceFodlers => [
        SourceContentPath,
        SourceStaticPath,
        SourceThemePath
    ];

    /// <summary>
    /// List of all pages, including generated.
    /// </summary>
    public IEnumerable<IPage> Pages
    {
        get
        {
            pagesCache ??= OutputReferences.Values
                .Where(output => output is IPage page)
                .Select(output => (output as IPage)!)
                .OrderBy(page => -page.Weight);
            return pagesCache!;
        }
    }

    /// <summary>
    /// List of all pages, including generated, by their permalink.
    /// </summary>
    public ConcurrentDictionary<string, IOutput> OutputReferences { get; } = new();

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public IEnumerable<IPage> RegularPages
    {
        get
        {
            regularPagesCache ??= OutputReferences
                    .Where(pair => pair.Value is IPage page && page.IsPage && pair.Key == page.Permalink)
                    .Select(pair => (pair.Value as IPage)!)
                    .OrderBy(page => -page.Weight);
            return regularPagesCache;
        }
    }

    /// <summary>
    /// The page of the home page;
    /// </summary>
    public IPage? Home { get; private set; }

    /// <summary>
    /// Manage all caching lists for the site
    /// </summary>
    public SiteCacheManager CacheManager { get; } = new();

    /// <summary>
    /// The Fluid parser instance.
    /// </summary>
    public FluidParser FluidParser { get; } = new();

    /// <summary>
    /// The Fluid/Liquid template options.
    /// </summary>
    public TemplateOptions TemplateOptions { get; } = new();

    /// <summary>
    /// The logger instance.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Number of files parsed, used in the report.
    /// </summary>
    public int FilesParsedToReport => filesParsedToReport;

    private int filesParsedToReport;

    private const string indexLeafFileConst = "index.md";

    private const string indexBranchFileConst = "_index.md";

    /// <summary>
    /// The synchronization lock object during ProstProcess.
    /// </summary>
    private readonly object syncLockPostProcess = new();

    /// <summary>
    /// The front matter parser instance. The default is YAML.
    /// </summary>
    private readonly IMetadataParser frontMatterParser;

    private IEnumerable<IPage>? pagesCache;

    private IEnumerable<IPage>? regularPagesCache;

    private readonly SiteSettings settings;

    /// <summary>
    /// Datetime wrapper
    /// </summary>
    private readonly ISystemClock clock;

    /// <summary>
    /// Constructor
    /// </summary>
    public Site(
        in IGenerateOptions options,
        in SiteSettings settings,
        in IMetadataParser frontMatterParser,
        in ILogger logger, ISystemClock? clock)
    {
        Options = options;
        this.settings = settings;
        Logger = logger;
        this.frontMatterParser = frontMatterParser;

        // Liquid template options, needed to theme the content 
        // but also parse URLs
        TemplateOptions.MemberAccessStrategy.Register<Site>();
        TemplateOptions.MemberAccessStrategy.Register<Page>();
        TemplateOptions.MemberAccessStrategy.Register<Resource>();

        this.clock = clock ?? new SystemClock();
    }

    /// <summary>
    /// Resets the template cache to force a reload of all templates.
    /// </summary>
    public void ResetCache()
    {
        CacheManager.ResetCache();
        OutputReferences.Clear();
    }

    /// <summary>
    /// Search recursively for all markdown files in the content folder, then
    /// parse their content for front matter meta data and markdown.
    /// </summary>
    /// <param name="directory">Folder to scan</param>
    /// <param name="level">Folder recursive level</param>
    /// <param name="parent">Page of the upper directory</param>
    /// <returns></returns>
    public void ParseAndScanSourceFiles(string? directory, int level = 0, IPage? parent = null)
    {
        directory ??= SourceContentPath;

        var markdownFiles = Directory.GetFiles(directory, "*.md");

        ParseIndexPage(directory, level, ref parent, ref markdownFiles);

        _ = Parallel.ForEach(markdownFiles, filePath =>
        {
            _ = ParseSourceFile(filePath, parent);
        });

        var subdirectories = Directory.GetDirectories(directory);
        _ = Parallel.ForEach(subdirectories, subdirectory =>
        {
            ParseAndScanSourceFiles(subdirectory, level + 1, parent);
        });
    }

    /// <summary>
    /// Creates the page for the site index.
    /// </summary>
    /// <param name="relativePath">The relative path of the page.</param>
    /// <param name="title"></param>
    /// <param name="sectionName"></param>
    /// <param name="originalPage"></param>
    /// <returns>The created page for the index.</returns>
    public IPage CreateSystemPage(string relativePath, string title, string? sectionName = null, IPage? originalPage = null)
    {
        sectionName ??= "section";
        var isIndex = string.IsNullOrEmpty(relativePath);
        relativePath = Urlizer.Path(relativePath);
        FrontMatter frontMatter = new()
        {
            Kind = isIndex ? Kind.index : Kind.list,
            Section = isIndex ? "index" : sectionName,
            SourceRelativePath = Urlizer.Path(Path.Combine(relativePath, indexLeafFileConst)),
            SourceFullPath = Urlizer.Path(Path.Combine(SourceContentPath, relativePath, indexLeafFileConst)),
            Title = title,
            Type = isIndex ? "index" : sectionName,
            URL = relativePath
        };

        var id = frontMatter.URL;

        // Get or create the page
        var lazyPage = CacheManager.automaticContentCache.GetOrAdd(id, new Lazy<IPage>(() =>
        {
            IPage? parent = null;
            // Check if we need to create a section, even
            var sections = (frontMatter.SourceRelativePathDirectory ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length > 1)
            {
                parent = CreateSystemPage(sections[0], sections[0]);
            }

            var newPage = new Page(frontMatter, this)
            {
                BundleType = BundleType.branch
            };
            PostProcessPage(newPage, parent);
            return newPage;
        }));

        // get the page from the lazy object
        var page = lazyPage.Value;

        if (originalPage is null || string.IsNullOrEmpty(originalPage.Permalink))
        {
            return page;
        }

        if (page.Kind != Kind.index)
        {
            page.PagesReferences.Add(originalPage.Permalink);
        }

        // TODO: still too hardcoded to add the tags reference
        if (page.Type != "tags")
        {
            return page;
        }
        originalPage.TagsReference.Add(page);
        return page;
    }

    private void ParseIndexPage(string? directory, int level, ref IPage? parent, ref string[] markdownFiles)
    {
        var indexLeafBundlePage = markdownFiles.FirstOrDefault(file => Path.GetFileName(file) == indexLeafFileConst);

        var indexBranchBundlePage = markdownFiles.FirstOrDefault(file => Path.GetFileName(file) == indexBranchFileConst);

        IPage? page = null;
        if (indexLeafBundlePage is not null || indexBranchBundlePage is not null)
        {
            // Determine the file to use and the bundle type
            var selectedFile = indexLeafBundlePage ?? indexBranchBundlePage;
            var bundleType = selectedFile == indexLeafBundlePage ? BundleType.leaf : BundleType.branch;

            // Remove the selected file from markdownFiles
            markdownFiles = bundleType == BundleType.leaf
                ? Array.Empty<string>()
                : markdownFiles.Where(file => file != selectedFile).ToArray();

            page = ParseSourceFile(selectedFile!, parent, bundleType);
            if (page is null) return;

            if (level == 0)
            {
                _ = OutputReferences.TryRemove(page!.Permalink!, out _);
                page.Permalink = "/";
                page.Kind = Kind.index;

                _ = OutputReferences.GetOrAdd(page.Permalink, page);
                Home = page;
            }
            else
            {
                parent = page;
            }
        }
        else if (level == 0)
        {
            Home = CreateSystemPage(string.Empty, Title);
        }
        else if (level == 1 && directory is not null)
        {
            var section = new DirectoryInfo(directory).Name;
            parent = CreateSystemPage(section, section);
        }
    }

    private Page? ParseSourceFile(in string filePath, in IPage? parent, BundleType bundleType = BundleType.none)
    {
        Page? page = null;
        try
        {
            var frontMatter = frontMatterParser.ParseFrontmatterAndMarkdownFromFile(filePath, SourceContentPath)
                ?? throw new FormatException($"Error parsing front matter for {filePath}");

            if (IsValidPage(frontMatter, Options))
            {
                page = new(frontMatter, this)
                {
                    BundleType = bundleType
                };
                PostProcessPage(page, parent, true);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error parsing file {file}", filePath);
        }

        // Use interlocked to safely increment the counter in a multi-threaded environment
        _ = Interlocked.Increment(ref filesParsedToReport);

        return page;
    }

    /// <summary>
    /// Extra calculation and automatic data for each page.
    /// </summary>
    /// <param name="page">The given page to be processed</param>
    /// <param name="parent">The parent page, if any</param>
    /// <param name="overwrite"></param>
    public void PostProcessPage(in IPage page, IPage? parent = null, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(page);

        page.Parent = parent;
        page.Permalink = page.CreatePermalink();
        lock (syncLockPostProcess)
        {
            if (!OutputReferences.TryGetValue(page.Permalink, out var oldOutput) || overwrite)
            {
                page.PostProcess();

                // Replace the old page with the newly created one
                if (oldOutput is IPage oldpage && oldpage.PagesReferences is not null)
                {
                    foreach (var pageOld in oldpage.PagesReferences)
                    {
                        page.PagesReferences.Add(pageOld);
                    }
                }

                // Register the page for all urls
                foreach (var pageOutput in page.AllOutputURLs)
                {
                    _ = OutputReferences.TryAdd(pageOutput.Key, pageOutput.Value);
                }
            }
        }

        if (!string.IsNullOrEmpty(page.Section)
            && OutputReferences.TryGetValue('/' + page.Section!, out var output))
        {
            if (output is IPage section)
            {
                section.PagesReferences.Add(page.Permalink!);
            }
        }
    }

    /// <inheritdoc />
    public bool IsValidPage(in IFrontMatter frontMatter, IGenerateOptions? options)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        return IsValidDate(frontMatter, options)
            && (frontMatter.Draft is null || frontMatter.Draft == false || (options?.Draft ?? false));
    }

    /// <inheritdoc />
    public bool IsValidDate(in IFrontMatter frontMatter, IGenerateOptions? options)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        return (!IsDateExpired(frontMatter) || (options?.Expired ?? false))
            && (IsDatePublishable(frontMatter) || (options?.Future ?? false));
    }

    /// <summary>
    /// Check if the page is expired
    /// </summary>
    public bool IsDateExpired(in IFrontMatter frontMatter)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        return frontMatter.ExpiryDate is not null && frontMatter.ExpiryDate <= clock.Now;
    }

    /// <summary>
    /// Check if the page is publishable
    /// </summary>
    public bool IsDatePublishable(in IFrontMatter frontMatter)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        return frontMatter.GetPublishDate is null || frontMatter.GetPublishDate <= clock.Now;
    }
}