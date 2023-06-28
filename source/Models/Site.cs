using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
using Markdig;
using Serilog;
using SuCoS.Helper;
using SuCoS.Parser;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, primarily extracted from the app.yaml file.
/// </summary>
public class Site : IParams
{
    /// <summary>
    /// Site Title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The base URL that will be used to build internal links.
    /// </summary>
    public string BaseUrl { get; set; } = "./";

    /// <summary>
    /// The base path of the source site files.
    /// </summary>
    public string SourceDirectoryPath { get; set; } = "./";

    /// <summary>
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    public bool UglyURLs { get; set; } = false;

    /// <summary>
    /// The path of the content, based on the source path.
    /// </summary>
    public string SourceContentPath => Path.Combine(SourceDirectoryPath, "content");

    /// <summary>
    /// The path of the static content (that will be copied as is), based on the source path.
    /// </summary>
    public string SourceStaticPath => Path.Combine(SourceDirectoryPath, "static");

    /// <summary>
    /// The path of the static content (that will be copied as is), based on the source path.
    /// </summary>
    public string SourceThemePath => Path.Combine(SourceDirectoryPath, "theme");

    /// <summary>
    /// The path where the generated site files will be saved.
    /// </summary>
    public string OutputPath { get; set; } = "./";

    /// <summary>
    /// List of all pages, including generated.
    /// </summary>
    public List<Frontmatter> Pages
    {
        get
        {
            pagesCache ??= PagesDict.Values.ToList();
            return pagesCache!;
        }
    }

    /// <summary>
    /// Expose a page getter to templates.
    /// </summary>
    /// <param name="permalink"></param>
    /// <returns></returns>
    public Frontmatter? GetPage(string permalink)
    {
        return PagesDict.TryGetValue(permalink, out var page) ? page : null;
    }

    /// <summary>
    /// List of all pages, including generated, by their permalink.
    /// </summary>
    public Dictionary<string, Frontmatter> PagesDict { get; } = new();

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public List<Frontmatter> RegularPages
    {
        get
        {
            regularPagesCache ??= PagesDict
                    .Where(pair => pair.Value.Kind == Kind.single)
                    .Select(pair => pair.Value)
                    .ToList();
            return regularPagesCache;
        }
    }

    /// <summary>
    /// The frontmatter of the home page;
    /// </summary>
    public Frontmatter? HomePage { get; set; }

    /// <summary>
    /// List of all content to be scanned and processed.
    /// </summary>
    public List<(string filePath, string content)> RawPages { get; set; } = new();

    /// <summary>
    /// Command line options
    /// </summary>
    public IGenerateOptions? options;

    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = new();

    #endregion IParams

    /// <summary>
    /// Cache for content templates.
    /// </summary>
    public readonly Dictionary<(string, Kind, string), string> contentTemplateCache = new();

    /// <summary>
    /// Cache for base templates.
    /// </summary>
    public readonly Dictionary<(string, Kind, string), string> baseTemplateCache = new();

    /// <summary>
    /// The Fluid parser instance.
    /// </summary>
    public readonly FluidParser FluidParser = new();

    /// <summary>
    /// The Fluid/Liquid template options.
    /// </summary>
    public readonly TemplateOptions TemplateOptions = new();

    /// <summary>
    /// The logger instance.
    /// </summary>
    public ILogger? Logger;

    /// <summary>
    /// The time that the older cache should be ignored.
    /// </summary>
    public DateTime IgnoreCacheBefore { get; set; }

    /// <summary>
    /// Cache for tag frontmatter.
    /// </summary>
    private readonly Dictionary<string, Frontmatter> automaticContentCache = new();

    /// <summary>
    /// The synchronization lock object.
    /// </summary>
    private readonly object syncLock = new();

    /// <summary>
    /// The synchronization lock object during ProstProcess.
    /// </summary>
    private readonly object syncLockPostProcess = new();

    /// <summary>
    /// The frontmatter parser instance. The default is YAML.
    /// </summary>
    private readonly IFrontmatterParser frontmatterParser = new YAMLParser();

    private List<Frontmatter>? pagesCache;

    private List<Frontmatter>? regularPagesCache;

    private readonly ISystemClock clock;

    /// <summary>
    /// Markdig 20+ built-in extensions
    /// </summary>
    /// https://github.com/xoofx/markdig
    public readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

    /// <summary>
    /// Constructor
    /// </summary>
    public Site() : this(new SystemClock())
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Site(ISystemClock clock)
    {
        // Liquid template options, needed to theme the content 
        // but also parse URLs
        TemplateOptions.MemberAccessStrategy.Register<Frontmatter>();
        TemplateOptions.MemberAccessStrategy.Register<Site>();

        this.clock = clock;
    }

    /// <summary>
    /// Scans all markdown files in the source directory.
    /// </summary>
    public void ScanAllMarkdownFiles()
    {
        // Scan content files
        var markdownFiles = FileUtils.GetAllMarkdownFiles(SourceContentPath);

        foreach (var fileAbsolutePath in markdownFiles)
        {
            var content = File.ReadAllText(fileAbsolutePath);
            var relativePath = Path.GetRelativePath(SourceContentPath, fileAbsolutePath);
            RawPages.Add((relativePath, content));
        }
    }

    /// <summary>
    /// Resets the template cache to force a reload of all templates.
    /// </summary>
    public void ResetCache()
    {
        baseTemplateCache.Clear();
        contentTemplateCache.Clear();
        automaticContentCache.Clear();
        PagesDict.Clear();
        IgnoreCacheBefore = DateTime.Now;
    }

    /// <summary>
    /// Create a page not from the content folder, but as part of the process.
    /// It's used to create tag pages, section list pages, etc.
    /// </summary>
    public Frontmatter CreateAutomaticFrontmatter(BasicContent baseContent, Frontmatter originalFrontmatter)
    {
        if (baseContent is null)
        {
            throw new ArgumentNullException(nameof(baseContent));
        }
        if (originalFrontmatter is null)
        {
            throw new ArgumentNullException(nameof(originalFrontmatter));
        }

        var id = baseContent.URL ?? "";
        Frontmatter? frontmatter = null;
        lock (syncLock)
        {
            if (!automaticContentCache.TryGetValue(id, out frontmatter))
            {
                frontmatter = new(
                    clock: new SystemClock(),
                    site: this,
                    title: baseContent.Title,
                    sourcePath: string.Empty,
                    sourceFileNameWithoutExtension: string.Empty,
                    sourcePathDirectory: null
                )
                {
                    Section = baseContent.Section,
                    Kind = baseContent.Kind,
                    Type = baseContent.Type,
                    URL = baseContent.URL,
                    PagesReferences = new()
                };

                automaticContentCache.Add(id, frontmatter);
                PostProcessFrontMatter(frontmatter);
            }
        }

        if (frontmatter.Kind != Kind.index && originalFrontmatter.Permalink is not null)
        {
            frontmatter.PagesReferences!.Add(originalFrontmatter.Permalink!);
        }

        // TODO: still too hardcoded
        if (frontmatter.Type == "tags")
        {
            lock (originalFrontmatter!)
            {
                if (frontmatter.Type == "tags")
                {
                    originalFrontmatter.Tags ??= new();
                    originalFrontmatter.Tags!.Add(frontmatter);
                }
            }
        }

        return frontmatter;
    }

    /// <summary>
    /// Parses the source files and extracts the frontmatter.
    /// </summary>
    public void ParseSourceFiles(StopwatchReporter stopwatch)
    {
        if (stopwatch is null)
        {
            throw new ArgumentNullException(nameof(stopwatch));
        }

        stopwatch.Start("Parse");

        // Process the source files, extracting the frontmatter
        var filesParsed = 0; // counter to keep track of the number of files processed
        _ = Parallel.ForEach(RawPages, file =>
        {
            try
            {
                var frontmatter = ReadSourceFrontmatter(file.filePath, file.content, frontmatterParser);

                if (frontmatter.IsValidDate(options))
                {
                    PostProcessFrontMatter(frontmatter, true);
                }
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "Error parsing file {file}", file.filePath);
            }

            // Use interlocked to safely increment the counter in a multi-threaded environment
            _ = Interlocked.Increment(ref filesParsed);
        });

        // If the home page is not yet created, create it!
        if (!PagesDict.TryGetValue("/", out var home))
        {
            home = CreateIndexPage(string.Empty);
        }
        HomePage = home;
        home.Kind = Kind.index;

        stopwatch.Stop("Parse", filesParsed);
    }

    /// <summary>
    /// Creates the frontmatter for the index page.
    /// </summary>
    /// <param name="relativePath">The relative path of the page.</param>
    /// <returns>The created frontmatter for the index page.</returns>
    private Frontmatter CreateIndexPage(string relativePath)
    {
        Frontmatter frontmatter = new(
            title: Title,
            site: this,
            clock: clock,
            sourcePath: Path.Combine(relativePath, "index.md"),
            sourceFileNameWithoutExtension: "index",
            sourcePathDirectory: "/"
        )
        {
            Kind = string.IsNullOrEmpty(relativePath) ? Kind.index : Kind.list,
            Section = (string.IsNullOrEmpty(relativePath) ? Kind.index : Kind.list).ToString()
        };

        PostProcessFrontMatter(frontmatter);
        return frontmatter;
    }

    /// <summary>
    /// Extra calculation and automatic data for each frontmatter.
    /// </summary>
    /// <param name="frontmatter"></param>
    /// <param name="overwrite"></param>
    public void PostProcessFrontMatter(Frontmatter frontmatter, bool overwrite = false)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }

        frontmatter.Permalink = frontmatter.CreatePermalink();
        lock (syncLockPostProcess)
        {
            if (!PagesDict.TryGetValue(frontmatter.Permalink, out var old) || overwrite)
            {
                if (old is not null)
                {
                    if (old?.PagesReferences is not null)
                    {
                        frontmatter.PagesReferences ??= new();
                        foreach (var page in old.PagesReferences)
                        {
                            frontmatter.PagesReferences.Add(page);
                        }
                    }
                }

                if (frontmatter.Aliases is not null)
                {
                    for (var i = 0; i < frontmatter.Aliases.Count; i++)
                    {
                        frontmatter.Aliases[i] = "/" + frontmatter.CreatePermalink(frontmatter.Aliases[i]);
                    }
                }

                // Register the page for all urls
                foreach (var url in frontmatter.Urls)
                {
                    PagesDict[url] = frontmatter;
                }
            }
        }

        // Create a section page when due
        if (frontmatter.Type != "section" && !string.IsNullOrEmpty(frontmatter.Permalink))
        {
            var contentTemplate = new BasicContent(
                title: frontmatter.Section,
                section: "section",
                type: "section",
                url: frontmatter.Section
            );
            CreateAutomaticFrontmatter(contentTemplate, frontmatter);
        }
    }

    /// <summary>
    /// Reads the frontmatter from the source file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="content">The file content.</param>
    /// <param name="frontmatterParser">The frontmatter parser.</param>
    /// <returns>The parsed frontmatter.</returns>
    private Frontmatter ReadSourceFrontmatter(string filePath, string content, IFrontmatterParser frontmatterParser)
    {
        // test if filePath or config is null
        if (filePath is null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }
        if (frontmatterParser is null)
        {
            throw new ArgumentNullException(nameof(frontmatterParser));
        }

        // Separate the YAML frontmatter from the file content
        var frontmatter = frontmatterParser.ParseFrontmatter(this, filePath, ref content)
            ?? throw new FormatException($"Error parsing frontmatter for {filePath}");

        return frontmatter;
    }
}
