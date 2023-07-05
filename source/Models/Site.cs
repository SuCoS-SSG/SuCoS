using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fluid;
using Markdig;
using Serilog;
using SuCoS.Parser;
using YamlDotNet.Serialization;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, primarily extracted from the app.yaml file.
/// </summary>
public class Site : IParams
{
    #region IParams

    /// <inheritdoc/>
    [YamlIgnore]
    public Dictionary<string, object> Params { get; set; } = new();

    #endregion IParams

    /// <summary>
    /// Site Title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The base URL that will be used to build internal links.
    /// </summary>
    public string BaseUrl { get; set; } = "./";

    /// <summary>
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    public bool UglyURLs { get; set; } = false;

    /// <summary>
    /// The base path of the source site files.
    /// </summary>
    [YamlIgnore]
    public string SourceDirectoryPath { get; set; } = "./";

    /// <summary>
    /// The path of the content, based on the source path.
    /// </summary>
    public string SourceContentPath => Path.Combine(SourceDirectoryPath, "content");

    /// <summary>
    /// The path of the static content (that will be copied as is), based on the source path.
    /// </summary>
    public string SourceStaticPath => Path.Combine(SourceDirectoryPath, "static");

    /// <summary>
    /// The path theme.
    /// </summary>
    public string SourceThemePath => Path.Combine(SourceDirectoryPath, "theme");

    /// <summary>
    /// The path of the static content (that will be copied as is), based on the theme path.
    /// </summary>
    public string SourceThemeStaticPath => Path.Combine(SourceThemePath, "static");

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
    public Frontmatter? Home { get; private set; }

    /// <summary>
    /// Command line options
    /// </summary>
    public IGenerateOptions? options;

    /// <summary>
    /// Cache for content templates.
    /// </summary>
    public readonly Dictionary<(string?, Kind?, string?), string> contentTemplateCache = new();

    /// <summary>
    /// Cache for base templates.
    /// </summary>
    public readonly Dictionary<(string?, Kind?, string?), string> baseTemplateCache = new();

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
    public DateTime IgnoreCacheBefore { get; private set; }

    /// <summary>
    /// Datetime wrapper
    /// </summary>
    public readonly ISystemClock Clock;

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

    /// <summary>
    /// Number of files parsed, used in the report.
    /// </summary>
    public int filesParsedToReport;

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

        Clock = clock;
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
    /// Search recursively for all markdown files in the content folder, then
    /// parse their content for front matter meta data and markdown.
    /// </summary>
    /// <param name="directory">Folder to scan</param>
    /// <param name="level">Folder recursive level</param>
    /// <param name="pageParent">Page of the upper directory</param>
    /// <returns></returns>
    public void ParseAndScanSourceFiles(string directory, int level = 0, Frontmatter? pageParent = null)
    {
        directory ??= SourceContentPath;

        var markdownFiles = Directory.GetFiles(directory, "*.md");

        var indexPath = markdownFiles.FirstOrDefault(file => Path.GetFileName(file).ToUpperInvariant() == "INDEX.MD");
        if (indexPath != null)
        {
            markdownFiles = markdownFiles.Where(file => file != indexPath).ToArray();
            var frontmatter = ParseSourceFile(pageParent, indexPath);
            if (level == 0)
            {
                Home = frontmatter;
                frontmatter!.Permalink = "/";
                PagesDict.Remove(frontmatter.Permalink);
                PagesDict.Add(frontmatter.Permalink, frontmatter);
            }
            else
            {
                pageParent = frontmatter;
            }
        }
        else if (level == 0)
        {
            Home = CreateIndexPage(string.Empty);
        }
        else if (level == 1)
        {
            var section = directory;
            var contentTemplate = new BasicContent(
                title: section,
                section: "section",
                type: "section",
                url: section
            );
            pageParent = CreateAutomaticFrontmatter(contentTemplate, null);
        }

        _ = Parallel.ForEach(markdownFiles, filePath =>
        {
            ParseSourceFile(pageParent, filePath);
        });

        var subdirectories = Directory.GetDirectories(directory);
        foreach (var subdirectory in subdirectories)
        {
            ParseAndScanSourceFiles(subdirectory, level + 1, pageParent);
        }
    }

    private Frontmatter? ParseSourceFile(Frontmatter? pageParent, string filePath)
    {
        Frontmatter? frontmatter = null;
        try
        {
            frontmatter = frontmatterParser.ParseFrontmatterAndMarkdownFromFile(this, filePath, SourceContentPath)
                ?? throw new FormatException($"Error parsing frontmatter for {filePath}");

            if (frontmatter.IsValidDate(options))
            {
                PostProcessFrontMatter(frontmatter, pageParent, true);
            }
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "Error parsing file {file}", filePath);
        }

        // Use interlocked to safely increment the counter in a multi-threaded environment
        _ = Interlocked.Increment(ref filesParsedToReport);

        return frontmatter;
    }

    /// <summary>
    /// Create a page not from the content folder, but as part of the process.
    /// It's used to create tag pages, section list pages, etc.
    /// </summary>
    public Frontmatter CreateAutomaticFrontmatter(BasicContent baseContent, Frontmatter? originalFrontmatter)
    {
        if (baseContent is null)
        {
            throw new ArgumentNullException(nameof(baseContent));
        }

        var id = baseContent.URL;
        Frontmatter? frontmatter;
        lock (syncLock)
        {
            if (!automaticContentCache.TryGetValue(id, out frontmatter))
            {
                frontmatter = new(
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

        if (frontmatter.Kind != Kind.index && originalFrontmatter?.Permalink is not null)
        {
            frontmatter.PagesReferences!.Add(originalFrontmatter.Permalink!);
        }


        // TODO: still too hardcoded
        if (frontmatter.Type != "tags" || originalFrontmatter is null)
        {
            return frontmatter;
        }
        lock (originalFrontmatter)
        {
            originalFrontmatter.Tags ??= new();
            originalFrontmatter.Tags!.Add(frontmatter);
        }
        return frontmatter;
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
    /// <param name="frontmatter">The given page to be processed</param>
    /// <param name="pageParent">The parent page, if any</param>
    /// <param name="overwrite"></param>
    public void PostProcessFrontMatter(Frontmatter frontmatter, Frontmatter? pageParent = null, bool overwrite = false)
    {
        if (frontmatter is null)
        {
            throw new ArgumentNullException(nameof(frontmatter));
        }

        frontmatter.Parent = pageParent;
        frontmatter.Permalink = frontmatter.CreatePermalink();
        lock (syncLockPostProcess)
        {
            if (!PagesDict.TryGetValue(frontmatter.Permalink, out var old) || overwrite)
            {
                if (old?.PagesReferences is not null)
                {
                    frontmatter.PagesReferences ??= new();
                    foreach (var page in old.PagesReferences)
                    {
                        frontmatter.PagesReferences.Add(page);
                    }
                }

                if (frontmatter.Aliases is not null)
                {
                    frontmatter.AliasesProcessed ??= new();
                    foreach (var alias in frontmatter.Aliases)
                    {
                        frontmatter.AliasesProcessed.Add(frontmatter.CreatePermalink(alias));
                    }
                }

                // Register the page for all urls
                foreach (var url in frontmatter.Urls)
                {
                    PagesDict[url] = frontmatter;
                }
            }
        }
    }
}