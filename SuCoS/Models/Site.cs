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
    public ConcurrentDictionary<string, IOutput> OutputReferences { get; } = [];

    /// <inheritdoc/>
    public IEnumerable<IPage> Pages
    {
        get
        {
            _pagesCache ??= OutputReferences.Values
                .Where(output => output is IPage)
                .Select(
                    output => (output as IPage)!)
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
                    pair.Value is IPage
                    {
                        IsPage: true
                    } page &&
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

    private readonly ConcurrentDictionary<string, FrontMatter> _frontMatters =
        [];

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
    public void ScanAndParseSourceFiles(IFileSystem fs, string? directory,
        int level = 0, FrontMatter? parent = null, FrontMatter? cascade = null)
    {
        ArgumentNullException.ThrowIfNull(fs);

        directory ??= SourceContentPath;

        cascade ??= new FrontMatter();

        var markdownFiles = fs.DirectoryGetFiles(directory, "*.md").ToList();
        ParseIndexFrontMatter(directory, level, ref parent, ref cascade,
            ref markdownFiles);

        // Other source files that are not index
        // _ = Parallel.ForEach(markdownFiles,
        markdownFiles.ForEach(
            filePath =>
            {
                var frontMatter = ParseFrontMatter(filePath, cascade);
                if (frontMatter is null)
                {
                    return;
                }

                frontMatter.FrontMatterParent = parent;

                FrontMatterAdd(frontMatter, cascade);
            });

        var subdirectories = fs.DirectoryGetDirectories(directory);
        subdirectories.ToList().ForEach(
            // _ = Parallel.ForEach(subdirectories,
            subdirectory =>
            {
                ScanAndParseSourceFiles(fs, subdirectory, level + 1, parent,
                    cascade);
            });
    }

    /// <inheritdoc/>
    public void PostProcessPage(in IPage page, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(page);

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
                foreach (var pageOutput in page.AllOutputUrLs.Where(
                             pageOutput => !OutputReferences.TryAdd(
                                 pageOutput.Key,
                                 pageOutput.Value)))
                {
                    Logger.Error(
                        "Duplicate permalink '{permalink}' from `{file}`. Was from '{from}'.",
                        pageOutput.Key,
                        (pageOutput.Value as Page)!.SourceRelativePath,
                        (OutputReferences[pageOutput.Key] as Page)!
                        .SourceRelativePath
                    );
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

    /// <summary>
    /// Create fake front matter for system-created pages
    /// </summary>
    /// <param name="relativePath"></param>
    /// <param name="title"></param>
    /// <param name="isTaxonomy"></param>
    private FrontMatter CreateSystemFrontMatter(
        string relativePath,
        string title,
        bool isTaxonomy = false)
    {
        relativePath = Urlizer.Path(relativePath);

        if (!CacheManager.AutomaticContentCache.TryGetValue(relativePath,
                out var frontMatter))
        {
            var directoryDepth = GetDirectoryDepth(relativePath);
            var sectionName = GetFirstDirectory(relativePath);
            var kind = directoryDepth switch
            {
                0 => Kind.home,
                1 => isTaxonomy ? Kind.taxonomy : Kind.section,
                _ => isTaxonomy ? Kind.term : Kind.list
            };

            frontMatter = new FrontMatter
            {
                Section = directoryDepth == 0 ? "index" : sectionName,
                SourceRelativePath = SourceRelativePath(relativePath),
                SourceFullPath = Urlizer.Path(Path.Combine(SourceContentPath,
                    relativePath, IndexBranchFileConst)),
                Title = title,
                Type = kind == Kind.home ? "index" : sectionName,
                Url = relativePath,
                BundleType = BundleType.Branch,
                Kind = kind
            };
            CacheManager.AutomaticContentCache.TryAdd(relativePath,
                frontMatter);
        }

        return frontMatter;
    }

    private static string SourceRelativePath(string? relativePath)
    {
        return Urlizer.Path(Path.Combine(relativePath ?? "", IndexBranchFileConst));
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

    private void ParseIndexFrontMatter(
        string? directory,
        int level,
        ref FrontMatter? parent,
        ref FrontMatter cascade,
        ref List<string> markdownFiles)
    {
        var indexLeafBundlePage = markdownFiles.FirstOrDefault(file =>
            Path.GetFileName(file) == IndexLeafFileConst);

        var indexBranchBundlePage = markdownFiles.FirstOrDefault(file =>
            Path.GetFileName(file) == IndexBranchFileConst);

        FrontMatter? frontMatter = null;

        var hasIndex = indexLeafBundlePage is not null ||
                       indexBranchBundlePage is not null;

        if (hasIndex)
        {
            // Determine the file to use and the bundle type
            var selectedFile = indexLeafBundlePage ?? indexBranchBundlePage;

            // Remove the selected file from markdownFiles
            markdownFiles = markdownFiles.Where(file =>
                file != indexLeafBundlePage &&
                file != indexBranchBundlePage).ToList();

            frontMatter = ParseFrontMatter(selectedFile!, cascade);
            if (frontMatter is null)
            {
                return;
            }

            frontMatter.BundleType = selectedFile == indexLeafBundlePage
                ? BundleType.Leaf
                : BundleType.Branch;

            // Use interlocked to safely increment the counter in a multithreaded environment
            _ = Interlocked.Increment(ref _filesParsedToReport);

            cascade = frontMatter.Cascade ?? cascade;
            frontMatter.FrontMatterParent = parent;
        }
        else
        {
            switch (level)
            {
                case 0:
                    frontMatter = CreateSystemFrontMatter(String.Empty, Title);
                    break;
                case 1:
                {
                    var section = new DirectoryInfo(directory!).Name;
                    frontMatter = CreateSystemFrontMatter(section, section);
                    break;
                }
            }
        }

        if (frontMatter is null)
        {
            return;
        }

        switch (level)
        {
            case 0:
                frontMatter.Kind = Kind.home;
                frontMatter.Url = "/";
                break;
            case 1:
                frontMatter.Kind = hasIndex ? frontMatter.Kind : Kind.section;
                frontMatter.Type ??= "section";
                parent = frontMatter;
                break;
            default:
                parent = frontMatter;
                break;
        }

        FrontMatterAdd(frontMatter);
    }

    /// <inheritdoc />
    public FrontMatter? FrontMatterAdd(FrontMatter? frontMatter,
        FrontMatter? cascade = null)
    {
        if (frontMatter is null)
        {
            return null;
        }

        if (!_frontMatters.TryAdd(frontMatter.SourceRelativePath!, frontMatter))
        {
            Logger.Error("Duplicate front matter found : {filepath}",
                frontMatter.SourceRelativePath);
        }

        var path = SourceRelativePath(frontMatter.Section);
        if (!string.IsNullOrEmpty(frontMatter.Section) && _frontMatters.TryGetValue(path,
                out var sectionFrontMatter))
        {
            LinkContent(frontMatter, sectionFrontMatter, false);
        }

        GenerateTags(frontMatter);
        return frontMatter;
    }

    private FrontMatter? ParseFrontMatter(in string fileFullPath,
        FrontMatter? cascade)
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

    // TODO: taxonomy should be customizable
    private void GenerateTags(FrontMatter frontMatter)
    {
        if (frontMatter.Tags == null)
        {
            return;
        }

        if (!_frontMatters.TryGetValue($"tags/_index.md",
                out var tagsFrontMatter))
        {
            tagsFrontMatter = CreateSystemFrontMatter("tags", "Tags");
            if (!_frontMatters.TryAdd(
                    tagsFrontMatter.SourceRelativePath!,
                    tagsFrontMatter))
            {
                Log.Error("already exist!");
            }
        }
        LinkContent(frontMatter, tagsFrontMatter, false);

        foreach (var tag in frontMatter.Tags)
        {
            var path = SourceRelativePath(Path.Combine("tags", tag));
            if (!_frontMatters.TryGetValue(path, out var tagFrontMatter))
            {
                tagFrontMatter =
                    CreateSystemFrontMatter(Path.Combine("tags", tag), tag);
                tagFrontMatter.FrontMatterParent = tagsFrontMatter;
                _frontMatters.TryAdd(tagFrontMatter.SourceRelativePath!,
                    tagFrontMatter);
            }

            LinkContent(frontMatter, tagFrontMatter, true);
        }
    }

    private static void LinkContent(FrontMatter content1, FrontMatter content2,
        bool isTag)
    {
        if (isTag)
        {
            content1.FrontMatterTagsReference.Add(content2);
        }

        content2.PagePages.Add(content1);
    }

    /// <summary>
    /// Create a Page from front matter
    /// </summary>
    /// <param name="frontMatter"></param>
    public Page? PageCreate(IFrontMatter frontMatter)
    {
        ArgumentNullException.ThrowIfNull(frontMatter);

        // Create the parent if it does not exist
        if (frontMatter.FrontMatterParent is FrontMatter
            {
                FrontMatterPages.Count: 0
            })
        {
            PageCreate(frontMatter.FrontMatterParent);
        }

        Page? page = null;
        if (IsPageValid(frontMatter, Options))
        {
            page = new(frontMatter, this);
            PostProcessPage(page, true);
            frontMatter.FrontMatterPages.Add(page);

            if (Home is null &&
                page.SourceRelativePath is IndexBranchFileConst
                    or IndexLeafFileConst)
            {
                Home = page;
            }
        }

        // Use interlocked to safely increment the counter in a multithreaded environment
        _ = Interlocked.Increment(ref _filesParsedToReport);

        return page;
    }

    /// <summary>
    /// Create pages from front matter
    /// </summary>
    public void ProcessPages() =>
        _frontMatters
            .Where(fm => fm.Value.FrontMatterPages.Count == 0)
            .OrderBy(fm =>
                !fm.Value.SourceRelativePath!.EndsWith("index.md",
                    StringComparison.OrdinalIgnoreCase))
            .ThenBy(fm => fm.Value.SourceRelativePathDirectory)
            .Select(fm => fm.Value)
            .ToList()
            .ForEach(fm => PageCreate(fm));
}
