using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
using Serilog;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parser;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, primarily extracted from the app.yaml file.
/// </summary>
internal class Site : ISite
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
    /// Site Title.
    /// </summary>
    public string Title => settings.Title;

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
    /// List of all pages, including generated.
    /// </summary>
    public IEnumerable<IPage> Pages
    {
        get
        {
            pagesCache ??= PagesReferences.Values
                .OrderBy(page => -page.Weight)
                .ToList();
            return pagesCache!;
        }
    }

    /// <summary>
    /// List of all pages, including generated, by their permalink.
    /// </summary>
    public ConcurrentDictionary<string, IPage> PagesReferences { get; } = new();

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public List<IPage> RegularPages
    {
        get
        {
            regularPagesCache ??= PagesReferences
                    .Where(pair => pair.Value.IsPage && pair.Key == pair.Value.Permalink)
                    .Select(pair => pair.Value)
                    .OrderBy(page => -page.Weight)
                    .ToList();
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
    public int filesParsedToReport;

    private const string indexFileConst = "index.md";

    private const string indexFileUpperConst = "INDEX.MD";

    /// <summary>
    /// The synchronization lock object during ProstProcess.
    /// </summary>
    private readonly object syncLockPostProcess = new();

    /// <summary>
    /// The front matter parser instance. The default is YAML.
    /// </summary>
    private readonly IFrontMatterParser frontMatterParser;

    private List<IPage>? pagesCache;

    private List<IPage>? regularPagesCache;

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
        in IFrontMatterParser frontMatterParser,
        in ILogger logger, ISystemClock? clock)
    {
        Options = options;
        this.settings = settings;
        Logger = logger;
        this.frontMatterParser = frontMatterParser;

        // Liquid template options, needed to theme the content 
        // but also parse URLs
        TemplateOptions.MemberAccessStrategy.Register<Page>();
        TemplateOptions.MemberAccessStrategy.Register<Site>();

        this.clock = clock ?? new SystemClock();
    }

    /// <summary>
    /// Resets the template cache to force a reload of all templates.
    /// </summary>
    public void ResetCache()
    {
        CacheManager.ResetCache();
        PagesReferences.Clear();
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
            ParseSourceFile(parent, filePath);
        });

        var subdirectories = Directory.GetDirectories(directory);
        _ = Parallel.ForEach(subdirectories, subdirectory =>
        {
            ParseAndScanSourceFiles(subdirectory, level + 1, parent);
        });
    }

    private void ParseIndexPage(in string? directory, int level, ref IPage? parent, ref string[] markdownFiles)
    {
        // Check if the index.md file exists in the current directory
        var indexPage = markdownFiles.FirstOrDefault(file => Path.GetFileName(file).ToUpperInvariant() == indexFileUpperConst);
        if (indexPage is not null)
        {
            markdownFiles = markdownFiles.Where(file => file != indexPage).ToArray();
            var page = ParseSourceFile(parent, indexPage);
            if (level == 0)
            {
                PagesReferences.TryRemove(page!.Permalink!, out _);
                Home = page;
                page.Permalink = "/";
                page.Kind = Kind.index;
                PagesReferences.GetOrAdd(page.Permalink, page);
            }
            else
            {
                parent = page;
            }
        }

        // If it's the home page
        else if (level == 0)
        {
            Home = CreateSystemPage(string.Empty, Title);
        }

        // Or a section page, which must be used as the parent for the next sub folder
        else if (level == 1)
        {
            var section = new DirectoryInfo(directory!).Name;
            parent = CreateSystemPage(section, section);
        }
    }

    private IPage? ParseSourceFile(in IPage? parent, in string filePath)
    {
        Page? page = null;
        try
        {
            var frontMatter = frontMatterParser.ParseFrontmatterAndMarkdownFromFile(filePath, SourceContentPath)
                ?? throw new FormatException($"Error parsing front matter for {filePath}");

            if (IsValidDate(frontMatter, Options))
            {
                page = new(frontMatter, this);
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
    /// Creates the page for the site index.
    /// </summary>
    /// <param name="relativePath">The relative path of the page.</param>
    /// <param name="title"></param>
    /// <param name="sectionName"></param>
    /// <param name="originalPage"></param>
    /// <returns>The created page for the index.</returns>
    private IPage CreateSystemPage(string relativePath, string title, string? sectionName = null, IPage? originalPage = null)
    {
        sectionName ??= "section";
        var isIndex = string.IsNullOrEmpty(relativePath);
        FrontMatter frontMatter = new()
        {
            Kind = isIndex ? Kind.index : Kind.list,
            Section = isIndex ? "index" : sectionName,
            SourcePath = Path.Combine(relativePath, indexFileConst),
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
            var sections = (frontMatter.SourcePathDirectory ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (sections.Length > 1)
            {
                parent = CreateSystemPage(sections[0], sections[0]);
            }

            var newPage = new Page(frontMatter, this);
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

    /// <summary>
    /// Extra calculation and automatic data for each page.
    /// </summary>
    /// <param name="page">The given page to be processed</param>
    /// <param name="parent">The parent page, if any</param>
    /// <param name="overwrite"></param>
    public void PostProcessPage(in IPage page, IPage? parent = null, bool overwrite = false)
    {
        if (page is null)
        {
            throw new ArgumentNullException(nameof(page));
        }

        page.Parent = parent;
        page.Permalink = page.CreatePermalink();
        lock (syncLockPostProcess)
        {
            if (!PagesReferences.TryGetValue(page.Permalink, out var old) || overwrite)
            {
                if (old?.PagesReferences is not null)
                {
                    foreach (var pageOld in old.PagesReferences)
                    {
                        page.PagesReferences.Add(pageOld);
                    }
                }

                if (page.Aliases is not null)
                {
                    page.AliasesProcessed ??= new();
                    foreach (var alias in page.Aliases)
                    {
                        page.AliasesProcessed.Add(page.CreatePermalink(alias));
                    }
                }

                // Register the page for all urls
                foreach (var url in page.Urls)
                {
                    PagesReferences.TryAdd(url, page);
                }
            }
        }

        if (page.Tags is not null)
        {
            foreach (var tagName in page.Tags)
            {
                CreateSystemPage(Path.Combine("tags", tagName), tagName, "tags", page);
            }
        }

        if (!string.IsNullOrEmpty(page.Section)
            && PagesReferences.TryGetValue('/' + page.Section!, out var section))
        {
            section.PagesReferences.Add(page.Permalink!);
        }
    }

    /// <summary>
    /// Check if the page have a publishing date from the past.
    /// </summary>
    /// <param name="frontMatter">Page or front matter</param>
    /// <param name="options">options</param>
    /// <returns></returns>
    public bool IsValidDate(in IFrontMatter frontMatter, IGenerateOptions? options)
    {
        if (frontMatter is null)
        {
            throw new ArgumentNullException(nameof(frontMatter));
        }
        return (!IsDateExpired(frontMatter) || (options?.Expired ?? false))
            && (IsDatePublishable(frontMatter) || (options?.Future ?? false));
    }

    /// <summary>
    /// Check if the page is expired
    /// </summary>
    public bool IsDateExpired(in IFrontMatter frontMatter)
    {
        if (frontMatter is null)
        {
            throw new ArgumentNullException(nameof(frontMatter));
        }
        return frontMatter.ExpiryDate is not null && frontMatter.ExpiryDate <= clock.Now;
    }

    /// <summary>
    /// Check if the page is publishable
    /// </summary>
    public bool IsDatePublishable(in IFrontMatter frontMatter)
    {
        if (frontMatter is null)
        {
            throw new ArgumentNullException(nameof(frontMatter));
        }
        return frontMatter.GetPublishDate is null || frontMatter.GetPublishDate <= clock.Now;
    }
}