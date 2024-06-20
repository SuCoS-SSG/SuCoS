using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Markdig;
using Microsoft.Extensions.FileSystemGlobbing;
using SuCoS.Helpers;

namespace SuCoS.Models;

/// <summary>
/// Each page data created from source files or from the system.
/// </summary>
public class Page : IPage
{
    private readonly IFrontMatter _frontMatter;

    #region IFrontMatter

    /// <inheritdoc/>
    public string? Title => _frontMatter.Title;

    /// <inheritdoc/>
    public string? Type => _frontMatter.Type;

    /// <inheritdoc/>
    public string? Url => _frontMatter.Url;

    /// <inheritdoc/>
    public bool? Draft => _frontMatter.Draft;

    /// <inheritdoc/>
    public List<string>? Aliases => _frontMatter.Aliases;

    /// <inheritdoc/>
    public string? Section => _frontMatter.Section;

    /// <inheritdoc/>
    public DateTime? Date => _frontMatter.Date;

    /// <inheritdoc/>
    public DateTime? LastMod => _frontMatter.LastMod;

    /// <inheritdoc/>
    public DateTime? PublishDate => _frontMatter.PublishDate;

    /// <inheritdoc/>
    public DateTime? ExpiryDate => _frontMatter.ExpiryDate;

    /// <inheritdoc/>
    public int Weight => _frontMatter.Weight;

    /// <inheritdoc/>
    public List<string>? Tags => _frontMatter.Tags;

    /// <inheritdoc/>
    public List<FrontMatterResources>? ResourceDefinitions
    {
        get => _frontMatter.ResourceDefinitions;
        set => _frontMatter.ResourceDefinitions = value;
    }

    /// <inheritdoc/>
    public string RawContent => _frontMatter.RawContent;

    /// <inheritdoc/>
    public Kind Kind { get; set; } = Kind.Single;

    /// <inheritdoc/>
    public string? SourceRelativePath => _frontMatter.SourceRelativePath;

    /// <inheritdoc/>
    public string? SourceRelativePathDirectory =>
        _frontMatter.SourceRelativePathDirectory;

    /// <inheritdoc/>
    public string SourceFullPath => _frontMatter.SourceFullPath;

    /// <inheritdoc/>
    public string? SourceFullPathDirectory =>
        _frontMatter.SourceFullPathDirectory;

    /// <inheritdoc/>
    public string? SourceFileNameWithoutExtension =>
        _frontMatter.SourceFileNameWithoutExtension;

    /// <inheritdoc/>
    public Dictionary<string, object> Params
    {
        get => _frontMatter.Params;
        set => _frontMatter.Params = value;
    }

    #endregion IFrontMatter

    /// <summary>
    /// The source directory of the file.
    /// </summary>
    public string? SourcePathLastDirectory =>
        string.IsNullOrEmpty(SourceRelativePathDirectory)
            ? null
            : Path.GetFileName(Path.GetFullPath(
                SourceRelativePathDirectory.TrimEnd(Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)));

    /// <summary>
    /// Point to the site configuration.
    /// </summary>
    public ISite Site { get; }

    /// <summary>
    /// Secondary URL patterns to be used to create the url.
    /// </summary>
    public Collection<string>? AliasesProcessed { get; set; }

    /// <inheritdoc/>
    public string? Permalink { get; set; }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    public ConcurrentBag<string> PagesReferences { get; } = [];

    /// <inheritdoc/>
    public IPage? Parent { get; set; }

    /// <inheritdoc/>
    public BundleType BundleType { get; set; } = BundleType.None;

    /// <inheritdoc/>
    public Collection<Resource>? Resources { get; set; }

    /// <summary>
    /// Plain markdown content, without HTML.
    /// </summary>
    public string Plain =>
        Markdown.ToPlainText(RawContent, SiteHelper.MarkdownPipeline);

    /// <summary>
    /// A list of tags, if any.
    /// </summary>
    public ConcurrentBag<IPage> TagsReference { get; } = [];

    /// <summary>
    /// Just a simple check if the current page is the home page
    /// </summary>
    public bool IsHome => Site.Home == this;

    /// <summary>
    /// Just a simple check if the current page is a section page
    /// </summary>
    public bool IsSection => Type == "section";

    /// <summary>
    /// Just a simple check if the current page is a "page"
    /// </summary>
    public bool IsPage => Kind == Kind.Single;

    /// <summary>
    /// The number of words in the main content
    /// </summary>
    public int WordCount => Plain
        .Split(NonWords, StringSplitOptions.RemoveEmptyEntries).Length;

    private static readonly char[] NonWords =
        [' ', ',', ';', '.', '!', '"', '(', ')', '?', '\n', '\r'];

    /// <summary>
    /// The markdown content converted to HTML
    /// </summary>
    public string ContentPreRendered => ContentPreRenderedCached.Value;

    /// <summary>
    /// The processed content.
    /// </summary>
    public string Content
    {
        get
        {
            ContentCache = ParseAndRenderTemplate(false,
                "Error rendering theme template: {Error}");
            return ContentCache!;
        }
    }

    /// <summary>
    /// Creates the output file by applying the theme templates to the page content.
    /// </summary>
    /// <returns>The processed output file content.</returns>
    public string CompleteContent =>
        ParseAndRenderTemplate(true, "Error parsing theme template: {Error}");

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    public IEnumerable<IPage> Pages
    {
        get
        {
            if (PagesCached is not null)
            {
                return PagesCached;
            }

            PagesCached = [];
            foreach (var permalink in PagesReferences)
            {
                if (Site.OutputReferences[permalink] is IPage page)
                {
                    PagesCached.Add(page);
                }
            }

            return PagesCached;
        }
    }

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public IEnumerable<IPage> RegularPages
    {
        get
        {
            _regularPagesCache ??= Pages
                .Where(page => page.IsPage)
                .ToList();
            return _regularPagesCache;
        }
    }

    /// <summary>
    /// Get all URLs related to this content.
    /// </summary>
    public Dictionary<string, IOutput> AllOutputUrLs
    {
        get
        {
            var urls = new Dictionary<string, IOutput>();

            if (Permalink is not null)
            {
                urls.Add(Permalink, this);
            }

            if (AliasesProcessed is not null)
            {
                foreach (var alias in AliasesProcessed)
                {
                    if (!urls.ContainsKey(alias))
                    {
                        urls.Add(alias, this);
                    }
                }
            }

            if (Resources is null)
            {
                return urls;
            }

            foreach (var resource in Resources)
            {
                if (resource.Permalink is not null)
                {
                    urls.TryAdd(resource.Permalink, resource);
                }
            }

            return urls;
        }
    }


    /// <summary>
    /// The markdown content.
    /// </summary>
    private Lazy<string> ContentPreRenderedCached => new(() =>
        Markdown.ToHtml(RawContent, SiteHelper.MarkdownPipeline));

    /// <summary>
    /// The cached content.
    /// </summary>
    private string? ContentCache { get; set; }

    private const string UrlForIndex = @"{%- liquid
if page.Parent
echo page.Parent.Permalink
echo '/'
endif
if page.Title != ''
echo page.Title
else
echo page.SourcePathLastDirectory
endif
-%}";

    private const string UrlForNonIndex = @"{%- liquid
if page.Parent
echo page.Parent.Permalink
echo '/'
endif
if page.Title != ''
echo page.Title
else
echo page.SourceFileNameWithoutExtension
endif
-%}";

    private List<IPage>? _regularPagesCache;

    private List<IPage>? PagesCached { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Page(in IFrontMatter frontMatter, in ISite site)
    {
        _frontMatter = frontMatter;
        Site = site;
    }

    /// <summary>
    /// Gets the Permalink path for the file.
    /// </summary>
    /// <param name="urlForce">The URL to consider. If null use the predefined URL</param>
    /// <returns>The output path.</returns>
    /// <see cref="UrlForIndex"/>
    /// <see cref="UrlForNonIndex"/>
    public string CreatePermalink(string? urlForce = null)
    {
        var isIndex = SourceFileNameWithoutExtension == "index";

        var permalink = string.Empty;

        urlForce ??= Url ?? (isIndex ? UrlForIndex : UrlForNonIndex);

        try
        {
            permalink = Site.TemplateEngine.Parse(urlForce, Site, this);
        }
        catch (Exception ex)
        {
            Site.Logger.Error(ex, "Error converting URL: {UrlForce}", urlForce);
        }

        if (!Path.IsPathRooted(permalink) && !permalink.StartsWith('/'))
        {
            permalink = $"/{permalink}";
        }

        return Urlizer.UrlizePath(permalink);
    }

    /// <inheritdoc/>
    public void PostProcess()
    {
        // Create all the aliases
        if (Aliases is not null)
        {
            AliasesProcessed ??= [];
            foreach (var alias in Aliases)
            {
                AliasesProcessed.Add(CreatePermalink(alias));
            }
        }

        // TODO: remove the hard coded
        // Create tag pages, if any
        if (Tags is not null)
        {
            Site.CreateSystemPage("tags", "Tags", true);
            foreach (var tagName in Tags)
            {
                Site.CreateSystemPage(Path.Combine("tags", tagName), tagName,
                    true, this);
            }
        }

        ScanForResources();
    }

    private int _counterInternal;
    private bool _counterInternalLock;

    private int Counter
    {
        get
        {
            if (!_counterInternalLock)
            {
                _counterInternalLock = true;
            }

            return _counterInternal;
        }
    }

    private void ScanForResources()
    {
        if (string.IsNullOrEmpty(SourceFullPathDirectory))
        {
            return;
        }

        if (BundleType == BundleType.None)
        {
            return;
        }

        if (!Directory.Exists(SourceFullPathDirectory))
        {
            return;
        }

        try
        {
            var resourceFiles = Directory.GetFiles(SourceFullPathDirectory)
                .Where(file =>
                    file != SourceFullPath &&
                    (BundleType == BundleType.Leaf || !file.EndsWith(".md",
                        StringComparison.OrdinalIgnoreCase))
                );

            foreach (var resourceFilename in resourceFiles)
            {
                Resources ??= [];
                var filenameOriginal = Path.GetFileName(resourceFilename);
                var filename = filenameOriginal;
                var extension = Path.GetExtension(resourceFilename);
                var title = filename;
                Dictionary<string, object> resourceParams = [];

                if (ResourceDefinitions is not null)
                {
                    if (_counterInternalLock)
                    {
                        _counterInternalLock = false;
                        ++_counterInternal;
                    }

                    foreach (var resourceDefinition in ResourceDefinitions)
                    {
                        resourceDefinition.GlobMatcher ??= new();
                        _ = resourceDefinition.GlobMatcher.AddInclude(
                            resourceDefinition.Src);
                        var file = new InMemoryDirectoryInfo("./",
                            new[] { filenameOriginal });
                        if (resourceDefinition.GlobMatcher.Execute(file)
                            .HasMatches)
                        {
                            filename =
                                Site.TemplateEngine.ParseResource(
                                    resourceDefinition.Name, Site, this,
                                    Counter) ?? filename;
                            title = Site.TemplateEngine.ParseResource(
                                resourceDefinition.Title, Site, this,
                                Counter) ?? filename;
                            resourceParams = resourceDefinition.Params;
                        }
                    }
                }

                filename = Path.GetFileNameWithoutExtension(filename) +
                           extension;
                var resource = new Resource()
                {
                    Title = title,
                    FileName = filename,
                    Permalink = Path.Combine(Permalink!, filename),
                    Params = resourceParams,
                    SourceFullPath = resourceFilename
                };
                Resources.Add(resource);
            }
        }
        catch
        {
            // ignored
        }
    }

    private string ParseAndRenderTemplate(bool isBaseTemplate,
        string errorMessage)
    {
        var fileContents = FileUtils.GetTemplate(Site.SourceThemePath, this,
            Site.CacheManager, isBaseTemplate);
        if (string.IsNullOrEmpty(fileContents))
        {
            return isBaseTemplate ? Content : ContentPreRendered;
        }

        try
        {
            return Site.TemplateEngine.Parse(fileContents, Site, this);
        }
        catch (FormatException ex)
        {
            Site.Logger.Error(ex, errorMessage);
            return string.Empty;
        }
    }
}
