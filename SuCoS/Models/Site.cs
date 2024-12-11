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
    public IFrontMatterParser Parser { get; }

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

    private readonly ConcurrentDictionary<string, ContentSource> _contentSources =
        [];

    /// <summary>
    /// Constructor
    /// </summary>
    public Site(
        in IGenerateOptions options,
        in SiteSettings settings,
        in IFrontMatterParser parser,
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
        int level = 0, ContentSource? parent = null, FrontMatter? cascade = null)
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
                var (frontMatter, rawContent) = ParseFile(filePath, cascade);
                if (frontMatter is null)
                {
                    return;
                }

                ContentSource contentSource = new(filePath, frontMatter, rawContent);
                contentSource.ContentSourceParent = parent;

                ContentSourceAdd(contentSource);
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
    private ContentSource CreateSystemContentSource(
        string relativePath,
        string title,
        bool isTaxonomy = false)
    {
        relativePath = Urlizer.Path(relativePath);

        if (!CacheManager.AutomaticContentCache.TryGetValue(relativePath,
                out var contentSource))
        {
            var directoryDepth = GetDirectoryDepth(relativePath);
            var sectionName = GetFirstDirectory(relativePath);
            var kind = directoryDepth switch
            {
                0 => Kind.home,
                1 => isTaxonomy ? Kind.taxonomy : Kind.section,
                _ => isTaxonomy ? Kind.term : Kind.list
            };

            var frontMatter = new FrontMatter
            {
                Section = directoryDepth == 0 ? "index" : sectionName,
                Title = title,
                Type = kind == Kind.home ? "index" : sectionName,
                Url = relativePath
            };
            contentSource = new ContentSource(SourceRelativePath(relativePath), frontMatter)
            {
                BundleType = BundleType.Branch,
                Kind = kind
            };
            CacheManager.AutomaticContentCache.TryAdd(relativePath,
                contentSource);
        }

        return contentSource;
    }

    private static string SourceRelativePath(string? relativePath)
    {
        return Urlizer.Path(Path.Combine(relativePath ?? "", IndexBranchFileConst));
    }

    /// <inheritdoc />
    public bool IsPageValid(in IContentSource contentSource,
        IGenerateOptions? options)
    {
        ArgumentNullException.ThrowIfNull(contentSource);

        return IsDateValid(contentSource, options)
               && (contentSource.Draft is null || contentSource.Draft == false ||
                   (options?.Draft ?? false));
    }

    /// <inheritdoc />
    public bool IsDateValid(in IContentSource contentSource,
        IGenerateOptions? options)
    {
        ArgumentNullException.ThrowIfNull(contentSource);

        return (!IsDateExpired(contentSource) || (options?.Expired ?? false))
               && (IsDatePublishable(contentSource) ||
                   (options?.Future ?? false));
    }

    /// <inheritdoc />
    public bool IsDateExpired(in IContentSource contentSource)
    {
        ArgumentNullException.ThrowIfNull(contentSource);

        return contentSource.ExpiryDate is not null &&
               contentSource.ExpiryDate <= _clock.Now;
    }

    /// <inheritdoc />
    public bool IsDatePublishable(in IContentSource contentSource)
    {
        ArgumentNullException.ThrowIfNull(contentSource);

        return contentSource.GetPublishDate is null ||
               contentSource.GetPublishDate <= _clock.Now;
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
        ref ContentSource? parent,
        ref FrontMatter cascade,
        ref List<string> markdownFiles)
    {
        var indexLeafBundlePage = markdownFiles.FirstOrDefault(file =>
            Path.GetFileName(file) == IndexLeafFileConst);

        var indexBranchBundlePage = markdownFiles.FirstOrDefault(file =>
            Path.GetFileName(file) == IndexBranchFileConst);

        ContentSource? contentSource = null;

        var hasIndex = indexLeafBundlePage is not null ||
                       indexBranchBundlePage is not null;

        if (hasIndex)
        {
            // Determine the file to use and the bundle type
            var selectedFile = indexLeafBundlePage ?? indexBranchBundlePage;

            var fileRelativePath = Path.GetRelativePath(SourceContentPath, selectedFile!);

            // Remove the selected file from markdownFiles
            markdownFiles = markdownFiles.Where(file =>
                file != indexLeafBundlePage &&
                file != indexBranchBundlePage).ToList();

            var (frontMatter, rawContent) = ParseFile(selectedFile!, cascade);

            if (frontMatter is null)
            {
                return;
            }
            contentSource = new(fileRelativePath!, frontMatter, rawContent)
            {
                BundleType = selectedFile == indexLeafBundlePage
                        ? BundleType.Leaf
                        : BundleType.Branch
            };

            // Use interlocked to safely increment the counter in a multithreaded environment
            _ = Interlocked.Increment(ref _filesParsedToReport);

            cascade = contentSource.FrontMatter.Cascade ?? cascade;
            contentSource.ContentSourceParent = parent;
        }
        else
        {
            switch (level)
            {
                case 0:
                    contentSource = CreateSystemContentSource(String.Empty, Title);
                    break;
                case 1:
                    {
                        var section = new DirectoryInfo(directory!).Name;
                        contentSource = CreateSystemContentSource(section, section);
                        break;
                    }
            }
        }

        if (contentSource is null)
        {
            return;
        }

        switch (level)
        {
            case 0:
                contentSource.Kind = Kind.home;
                contentSource.FrontMatter.Url = "/";
                break;
            case 1:
                contentSource.Kind = hasIndex ? contentSource.Kind : Kind.section;
                contentSource.Type ??= "section";
                parent = contentSource;
                break;
            default:
                parent = contentSource;
                break;
        }

        ContentSourceAdd(contentSource);
    }

    /// <inheritdoc />
    public ContentSource? ContentSourceAdd(ContentSource? contentSource)
    {
        if (contentSource is null)
        {
            return null;
        }

        if (!_contentSources.TryAdd(contentSource.SourceRelativePath!, contentSource))
        {
            Logger.Error("Duplicate front matter found : {filepath}",
                contentSource.SourceRelativePath);
        }

        var path = SourceRelativePath(contentSource.Section);
        if (!string.IsNullOrEmpty(contentSource.Section) && _contentSources.TryGetValue(path,
                out var sectionFrontMatter))
        {
            LinkContent(contentSource, sectionFrontMatter, false);
        }

        GenerateTags(contentSource);
        return contentSource;
    }

    private (FrontMatter?, string) ParseFile(in string fileFullPath, FrontMatter? cascade)
    {
        var fileRelativePath =
            Path.GetRelativePath(SourceContentPath, fileFullPath);
        try
        {
            var fileContent = File.ReadAllText(fileFullPath);
            var (frontMatter, rawContent) =
                FrontMatter.Parse(fileFullPath, fileRelativePath, Parser, fileContent);

            // throw new FormatException(
            // $"Error parsing front matter for {fileFullPath}");

            return (cascade is not null
                ? cascade.Merge(frontMatter)
                : frontMatter
                , rawContent);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error parsing file {file}", fileFullPath);
        }

        return (null, string.Empty);
    }

    // TODO: taxonomy should be customizable
    private void GenerateTags(ContentSource contentSource)
    {
        if (contentSource.Tags == null)
        {
            return;
        }

        if (!_contentSources.TryGetValue($"tags/_index.md",
                out var tagsContentSource))
        {
            tagsContentSource = CreateSystemContentSource("tags", "Tags");
            if (!_contentSources.TryAdd(
                    tagsContentSource.SourceRelativePath!,
                    tagsContentSource))
            {
                Log.Error("already exist!");
            }
        }
        LinkContent(contentSource, tagsContentSource, false);

        foreach (var tag in contentSource.Tags)
        {
            var path = SourceRelativePath(Path.Combine("tags", tag));
            if (!_contentSources.TryGetValue(path, out var tagFrontMatter))
            {
                tagFrontMatter =
                    CreateSystemContentSource(Path.Combine("tags", tag), tag);
                tagFrontMatter.ContentSourceParent = tagsContentSource;
                _contentSources.TryAdd(tagFrontMatter.SourceRelativePath!,
                    tagFrontMatter);
            }

            LinkContent(contentSource, tagFrontMatter, true);
        }
    }

    private static void LinkContent(ContentSource content1, ContentSource content2,
        bool isTag)
    {
        if (isTag)
        {
            content1.ContentSourceTags.Add(content2);
        }

        content2.PagePages.Add(content1);
    }

    /// <summary>
    /// Create a Page from front matter
    /// </summary>
    /// <param name="contentSource"></param>
    public Page? PageCreate(ContentSource contentSource)
    {
        ArgumentNullException.ThrowIfNull(contentSource);

        // Create the parent if it does not exist
        if (contentSource.ContentSourceParent is ContentSource
            {
                ContentSourceToPages.Count: 0
            })
        {
            PageCreate(contentSource.ContentSourceParent);
        }

        Page? page = null;
        if (IsPageValid(contentSource, Options))
        {
            page = new(contentSource, this);
            PostProcessPage(page, true);
            contentSource.ContentSourceToPages.Add(page);

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
        _contentSources
            .Where(fm => fm.Value.ContentSourceToPages.Count == 0)
            .OrderBy(fm =>
                !fm.Value.SourceRelativePath!.EndsWith("index.md",
                    StringComparison.OrdinalIgnoreCase))
            .ThenBy(fm => fm.Value.SourceRelativePathDirectory)
            .Select(fm => fm.Value)
            .ToList()
            .ForEach(fm => PageCreate(fm));
}
