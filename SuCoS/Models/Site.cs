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
public class Site : ISite
{
    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params
    {
        get => _settings.Params;
        set => _settings.Params = value;
    }

    #endregion IParams

    /// <summary>
    /// Command line options
    /// </summary>
    public IGenerateOptions Options { get; set; }

    #region SiteSettings

    /// <inheritdoc/>
    public string Title => _settings.Title;

    /// <inheritdoc/>
    public string? Description => _settings.Description;

    /// <inheritdoc/>
    public string? Copyright => _settings.Copyright;

    /// <inheritdoc/>
    public string BaseUrl => _settings.BaseUrl;

    /// <inheritdoc/>
    public bool UglyUrLs => _settings.UglyUrLs;

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
    public string SourceThemePath => Path.Combine(Options.Source, _settings.ThemeDir, _settings.Theme ?? string.Empty);

    /// <inheritdoc/>
    public IEnumerable<string> SourceFolders => [
        SourceContentPath,
        SourceStaticPath,
        SourceThemePath
    ];

    /// <summary>
    /// Theme used.
    /// </summary>
    public Theme? Theme { get; }

    /// <summary>
    /// List of all pages, including generated.
    /// </summary>
    public IEnumerable<IPage> Pages
    {
        get
        {
            _pagesCache ??= OutputReferences.Values
                .Where(output => output is IPage)
                .Select(output => (output as IPage)!)
                .OrderBy(page => -page.Weight);
            return _pagesCache!;
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
            _regularPagesCache ??= OutputReferences
                .Where(pair => pair.Value is IPage { IsPage: true } page && pair.Key == page.Permalink)
                .Select(pair => (pair.Value as IPage)!)
                .OrderBy(page => -page.Weight);
            return _regularPagesCache;
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
    /// The logger instance.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Number of files parsed, used in the report.
    /// </summary>
    public int FilesParsedToReport => _filesParsedToReport;

    /// <inheritdoc/>
    public IMetadataParser Parser { get; }

    private int _filesParsedToReport;

    private const string IndexLeafFileConst = "index.md";

    private const string IndexBranchFileConst = "_index.md";

    /// <summary>
    /// The synchronization lock object during PostProcess.
    /// </summary>
    private readonly object _syncLockPostProcess = new();

    private IEnumerable<IPage>? _pagesCache;

    private IEnumerable<IPage>? _regularPagesCache;

    private readonly SiteSettings _settings;

    /// <summary>
    /// Datetime wrapper
    /// </summary>
    private readonly ISystemClock _clock;

    /// <inheritdoc/>
    public ITemplateEngine TemplateEngine { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Site(
        in IGenerateOptions options,
        in SiteSettings settings,
        in IMetadataParser parser,
        in ILogger logger, ISystemClock? clock)
    {
        Options = options;
        _settings = settings;
        Logger = logger;
        Parser = parser;
        TemplateEngine = new FluidTemplateEngine();

        _clock = clock ?? new SystemClock();

        Theme = Theme.CreateFromSite(this);
    }

    /// <summary>
    /// Resets the template cache to force a reload of all templates.
    /// </summary>
    public void ResetCache()
    {
        CacheManager.ResetCache();
        OutputReferences.Clear();
    }

    /// <inheritdoc/>
    public void ParseAndScanSourceFiles(IFileSystem fs, string? directory, int level = 0, IPage? parent = null)
    {
        ArgumentNullException.ThrowIfNull(fs);

        directory ??= SourceContentPath;

        var markdownFiles = fs.DirectoryGetFiles(directory, "*.md");

        ParseIndexPage(directory, level, ref parent, ref markdownFiles);

        _ = Parallel.ForEach(markdownFiles, filePath =>
        {
            _ = ParseSourceFile(filePath, parent);
        });

        var subdirectories = fs.DirectoryGetDirectories(directory);
        _ = Parallel.ForEach(subdirectories, subdirectory =>
        {
            ParseAndScanSourceFiles(fs, subdirectory, level + 1, parent);
        });
    }

    /// <inheritdoc/>
    public IPage CreateSystemPage(string relativePath, string title, bool isTaxonomy = false, IPage? originalPage = null)
    {
        relativePath = Urlizer.Path(relativePath);
        relativePath = relativePath == "homepage" ? "/" : relativePath;

        var id = relativePath;

        // Get or create the page
        var lazyPage = CacheManager.AutomaticContentCache.GetOrAdd(id, new Lazy<IPage>(() =>
        {
            var directoryDepth = GetDirectoryDepth(relativePath);
            var sectionName = GetFirstDirectory(relativePath);
            var kind = directoryDepth switch
            {
                0 => Kind.Home,
                1 => isTaxonomy ? Kind.Taxonomy : Kind.Section,
                _ => isTaxonomy ? Kind.Term : Kind.List
            };

            FrontMatter frontMatter = new()
            {
                Section = directoryDepth == 0 ? "index" : sectionName,
                SourceRelativePath = Urlizer.Path(Path.Combine(relativePath, IndexLeafFileConst)),
                SourceFullPath = Urlizer.Path(Path.Combine(SourceContentPath, relativePath, IndexLeafFileConst)),
                Title = title,
                Type = kind == Kind.Home ? "index" : sectionName,
                Url = relativePath
            };

            IPage? parent = null;

            var newPage = new Page(frontMatter, this)
            {
                BundleType = BundleType.Branch,
                Kind = kind
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

        if (page.Kind != Kind.Home)
        {
            page.PagesReferences.Add(originalPage.Permalink);
        }

        // TODO: still too hardcoded to add the tags reference
        if ((page.Kind & Kind.IsTaxonomy) != Kind.IsTaxonomy)
        {
            return page;
        }
        originalPage.TagsReference.Add(page);
        return page;
    }

    private static string GetFirstDirectory(string relativePath) =>
        GetDirectories(relativePath).Length > 0
            ? GetDirectories(relativePath)[0]
            : string.Empty;

    private static int GetDirectoryDepth(string relativePath) =>
        GetDirectories(relativePath).Length;

    private static string[] GetDirectories(string? relativePath) =>
        (relativePath ?? string.Empty).Split('/',
            StringSplitOptions.RemoveEmptyEntries);

    private void ParseIndexPage(string? directory, int level, ref IPage? parent, ref string[] markdownFiles)
    {
        var indexLeafBundlePage = markdownFiles.FirstOrDefault(file => Path.GetFileName(file) == IndexLeafFileConst);

        var indexBranchBundlePage = markdownFiles.FirstOrDefault(file => Path.GetFileName(file) == IndexBranchFileConst);

        if (indexLeafBundlePage is not null || indexBranchBundlePage is not null)
        {
            // Determine the file to use and the bundle type
            var selectedFile = indexLeafBundlePage ?? indexBranchBundlePage;
            var bundleType = selectedFile == indexLeafBundlePage ? BundleType.Leaf : BundleType.Branch;

            // Remove the selected file from markdownFiles
            markdownFiles = bundleType == BundleType.Leaf
                ? [] : markdownFiles.Where(file => file != selectedFile).ToArray();

            IPage? page = ParseSourceFile(selectedFile!, parent, bundleType);
            if (page is null)
            {
                return;
            }

            if (level == 0)
            {
                _ = OutputReferences.TryRemove(page.Permalink!, out _);
                page.Permalink = "/";
                page.Kind = Kind.Home;

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

    private Page? ParseSourceFile(in string fileFullPath, in IPage? parent, BundleType bundleType = BundleType.None)
    {
        Page? page = null;
        try
        {
            var fileContent = File.ReadAllText(fileFullPath);
            var fileRelativePath = Path.GetRelativePath(
                SourceContentPath,
                fileFullPath
            );
            var frontMatter = FrontMatter.Parse(fileFullPath, fileRelativePath, Parser, fileContent)
                ?? throw new FormatException($"Error parsing front matter for {fileFullPath}");

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
            Logger.Error(ex, "Error parsing file {file}", fileFullPath);
        }

        // Use interlocked to safely increment the counter in a multithreaded environment
        _ = Interlocked.Increment(ref _filesParsedToReport);

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
        lock (_syncLockPostProcess)
        {
            if (!OutputReferences.TryGetValue(page.Permalink, out var oldOutput) || overwrite)
            {
                page.PostProcess();

                // Replace the old page with the newly created one
                if (oldOutput is IPage oldPage)
                {
                    foreach (var pageOld in oldPage.PagesReferences)
                    {
                        page.PagesReferences.Add(pageOld);
                    }
                }

                // Register the page for all urls
                foreach (var pageOutput in page.AllOutputUrLs)
                {
                    _ = OutputReferences.TryAdd(pageOutput.Key, pageOutput.Value);
                }
            }
        }

        if (!string.IsNullOrEmpty(page.Section)
            && OutputReferences.TryGetValue('/' + page.Section!, out var output)
            && (output is IPage section)
            && page.Kind != Kind.Section
            && page.Kind != Kind.Taxonomy)
        {
            section.PagesReferences.Add(page.Permalink!);
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

        return frontMatter.ExpiryDate is not null && frontMatter.ExpiryDate <= _clock.Now;
    }

    /// <summary>
    /// Check if the page is publishable
    /// </summary>
    public bool IsDatePublishable(in IFrontMatter frontMatter)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        return frontMatter.GetPublishDate is null || frontMatter.GetPublishDate <= _clock.Now;
    }
}
