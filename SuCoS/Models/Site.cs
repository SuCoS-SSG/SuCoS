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

    #region ISite

    /// <inheritdoc/>
    public Sucos SuCoS { get; } = new Sucos();

    /// <inheritdoc/>
    public IGenerateOptions Options { get; set; }

    /// <inheritdoc/>
    public Theme? Theme { get; }

    /// <inheritdoc/>
    public string SourceContentPath => Path.Combine(Options.Source, "content");

    /// <inheritdoc/>
    public string SourceStaticPath => Path.Combine(Options.Source, "static");

    /// <inheritdoc/>
    public string SourceThemePath => Path.Combine(Options.Source,
        _settings.ThemeDir, _settings.Theme ?? string.Empty);

    /// <inheritdoc/>
    public ConcurrentDictionary<string, IOutput> OutputReferences { get; } =
        new();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public IEnumerable<IPage> RegularPages
    {
        get
        {
            _regularPagesCache ??= OutputReferences
                .Where(pair =>
                    pair.Value is IPage { IsPage: true } page &&
                    pair.Key == page.Permalink)
                .Select(pair => (pair.Value as IPage)!)
                .OrderBy(page => -page.Weight);
            return _regularPagesCache;
        }
    }

    /// <inheritdoc/>
    public IPage? Home { get; private set; }

    /// <inheritdoc/>
    public SiteCacheManager CacheManager { get; } = new();

    /// <inheritdoc/>
    public IMetadataParser Parser { get; }

    /// <inheritdoc/>
    public ITemplateEngine TemplateEngine { get; }

    /// <inheritdoc/>
    public ILogger Logger { get; }

    /// <inheritdoc/>
    public IEnumerable<string> SourceFolders =>
    [
        SourceContentPath,
        SourceStaticPath,
        SourceThemePath
    ];

    /// <inheritdoc/>
    public void ResetCache()
    {
        CacheManager.ResetCache();
        OutputReferences.Clear();
    }

    #endregion

    /// <summary>
    /// Number of files parsed, used in the report.
    /// </summary>
    public int FilesParsedToReport => _filesParsedToReport;

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

    /// <summary>
    /// Constructor
    /// </summary>
    public Site(
        in IGenerateOptions options,
        in SiteSettings settings,
        in IMetadataParser parser,
        in ILogger logger,
        ISystemClock? clock)
    {
        Options = options;
        _settings = settings;
        Logger = logger;
        Parser = parser;
        TemplateEngine = new FluidTemplateEngine();

        _clock = clock ?? new SystemClock();

        Theme = Theme.CreateFromSite(this);
    }

    #region ISite methods

    /// <inheritdoc/>
    public void ParseAndScanSourceFiles(IFileSystem fs, string? directory,
        int level = 0, IPage? parent = null, FrontMatter? cascade = null)
    {
        ArgumentNullException.ThrowIfNull(fs);

        directory ??= SourceContentPath;

        var markdownFiles = fs.DirectoryGetFiles(directory, "*.md");

        cascade ??= new FrontMatter();
        ParseIndexPage(directory, level, ref parent, ref cascade,
            ref markdownFiles);

        _ = Parallel.ForEach(markdownFiles,
            filePath =>
            {
                var frontMatter = ParseFrontMatter(filePath, cascade);
                if (frontMatter is null)
                {
                    return;
                }

                PageCreate(frontMatter, parent);
            });

        var subdirectories = fs.DirectoryGetDirectories(directory);
        _ = Parallel.ForEach(subdirectories,
            subdirectory =>
            {
                ParseAndScanSourceFiles(fs, subdirectory, level + 1, parent,
                    cascade);
            });
    }

    /// <summary>
    /// Extra calculation and automatic data for each page.
    /// </summary>
    /// <param name="page">The given page to be processed</param>
    /// <param name="parent">The parent page, if any</param>
    /// <param name="overwrite"></param>
    public void PostProcessPage(in IPage page, IPage? parent = null,
        bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(page);

        page.Parent = parent;
        page.Permalink = page.CreatePermalink();
        lock (_syncLockPostProcess)
        {
            if (!OutputReferences.TryGetValue(page.Permalink,
                    out var oldOutput) || overwrite)
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
                    _ = OutputReferences.TryAdd(pageOutput.Key,
                        pageOutput.Value);
                }
            }
        }

        if (!string.IsNullOrEmpty(page.Section)
            && OutputReferences.TryGetValue('/' + page.Section!, out var output)
            && (output is IPage section)
            && page.Kind != Kind.section
            && page.Kind != Kind.taxonomy)
        {
            section.PagesReferences.Add(page.Permalink!);
        }
    }

    /// <inheritdoc/>
    public IPage CreateSystemPage(string relativePath, string title,
        bool isTaxonomy = false, IPage? originalPage = null)
    {
        relativePath = Urlizer.Path(relativePath);
        relativePath = relativePath == "homepage" ? "/" : relativePath;

        var id = relativePath;

        // Get or create the page
        var lazyPage = CacheManager.AutomaticContentCache.GetOrAdd(id,
            new Lazy<IPage>(() =>
            {
                var directoryDepth = GetDirectoryDepth(relativePath);
                var sectionName = GetFirstDirectory(relativePath);
                var kind = directoryDepth switch
                {
                    0 => Kind.home,
                    1 => isTaxonomy ? Kind.taxonomy : Kind.section,
                    _ => isTaxonomy ? Kind.term : Kind.list
                };

                FrontMatter frontMatter = new()
                {
                    Section = directoryDepth == 0 ? "index" : sectionName,
                    SourceRelativePath =
                        Urlizer.Path(Path.Combine(relativePath,
                            IndexLeafFileConst)),
                    SourceFullPath = Urlizer.Path(Path.Combine(
                        SourceContentPath, relativePath, IndexLeafFileConst)),
                    Title = title,
                    Type = kind == Kind.home ? "index" : sectionName,
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

        if (originalPage is null ||
            string.IsNullOrEmpty(originalPage.Permalink))
        {
            return page;
        }

        if (page.Kind != Kind.home)
        {
            page.PagesReferences.Add(originalPage.Permalink);
        }

        // TODO: still too hardcoded to add the tags reference
        if ((page.Kind & Kind.istaxonomy) != Kind.istaxonomy)
        {
            return page;
        }

        originalPage.TagsReference.Add(page);
        return page;
    }

    /// <inheritdoc />
    public bool IsPageValid(in IFrontMatter frontMatter,
        IGenerateOptions? options)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        return IsDateValid(frontMatter, options)
               && (frontMatter.Draft is null || frontMatter.Draft == false ||
                   (options?.Draft ?? false));
    }

    /// <inheritdoc />
    public bool IsDateValid(in IFrontMatter frontMatter,
        IGenerateOptions? options)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        return (!IsDateExpired(frontMatter) || (options?.Expired ?? false))
               && (IsDatePublishable(frontMatter) ||
                   (options?.Future ?? false));
    }

    /// <inheritdoc />
    public bool IsDateExpired(in IFrontMatter frontMatter)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        return frontMatter.ExpiryDate is not null &&
               frontMatter.ExpiryDate <= _clock.Now;
    }

    /// <inheritdoc />
    public bool IsDatePublishable(in IFrontMatter frontMatter)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        return frontMatter.GetPublishDate is null ||
               frontMatter.GetPublishDate <= _clock.Now;
    }

    #endregion ISite methods

    private static string GetFirstDirectory(string relativePath) =>
        GetDirectories(relativePath).Length > 0
            ? GetDirectories(relativePath)[0]
            : string.Empty;

    private static int GetDirectoryDepth(string relativePath) =>
        GetDirectories(relativePath).Length;

    private static string[] GetDirectories(string? relativePath) =>
        (relativePath ?? string.Empty).Split('/',
            StringSplitOptions.RemoveEmptyEntries);

    private void ParseIndexPage(string? directory, int level, ref IPage? parent,
        ref FrontMatter cascade, ref string[] markdownFiles)
    {
        var indexLeafBundlePage = markdownFiles.FirstOrDefault(file =>
            Path.GetFileName(file) == IndexLeafFileConst);

        var indexBranchBundlePage = markdownFiles.FirstOrDefault(file =>
            Path.GetFileName(file) == IndexBranchFileConst);

        if (indexLeafBundlePage is not null ||
            indexBranchBundlePage is not null)
        {
            // Determine the file to use and the bundle type
            var selectedFile = indexLeafBundlePage ?? indexBranchBundlePage;
            var bundleType = selectedFile == indexLeafBundlePage
                ? BundleType.Leaf
                : BundleType.Branch;

            // Remove the selected file from markdownFiles
            markdownFiles = bundleType == BundleType.Leaf
                ? []
                : markdownFiles.Where(file => file != selectedFile).ToArray();

            var frontMatter = ParseFrontMatter(selectedFile!, cascade);
            if (frontMatter is null)
            {
                return;
            }

            cascade = frontMatter.Cascade ?? cascade;

            IPage? page = PageCreate(frontMatter, parent, bundleType);
            if (page is null)
            {
                return;
            }

            if (level == 0)
            {
                _ = OutputReferences.TryRemove(page.Permalink!, out _);
                page.Permalink = "/";
                page.Kind = Kind.home;

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

    private FrontMatter? ParseFrontMatter(in string fileFullPath, FrontMatter? cascade)
    {
        var fileRelativePath =
            Path.GetRelativePath(SourceContentPath, fileFullPath);
        try
        {
            var fileContent = File.ReadAllText(fileFullPath);
            var frontMatter = FrontMatter.Parse(fileFullPath, fileRelativePath,
                                  Parser, fileContent)
                              ?? throw new FormatException(
                                  $"Error parsing front matter for {fileFullPath}");

            return cascade is not null
                ? cascade.Merge(frontMatter)
                : frontMatter;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error parsing file {file}", fileFullPath);
        }

        return null;
    }

    private Page? PageCreate(IFrontMatter frontMatter, in IPage? parent,
        BundleType bundleType = BundleType.None)
    {
        Page? page = null;

        if (IsPageValid(frontMatter, Options))
        {
            page = new(frontMatter, this)
            {
                BundleType = bundleType
            };
            PostProcessPage(page, parent, true);
        }

        // Use interlocked to safely increment the counter in a multithreaded environment
        _ = Interlocked.Increment(ref _filesParsedToReport);

        return page;
    }
}
