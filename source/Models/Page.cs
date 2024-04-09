using Fluid;
using Markdig;
using Microsoft.Extensions.FileSystemGlobbing;
using SuCoS.Helpers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace SuCoS.Models;

/// <summary>
/// Each page data created from source files or from the system.
/// </summary>
public class Page : IPage
{
    private readonly IFrontMatter frontMatter;

    #region IFrontMatter

    /// <inheritdoc/>
    public string? Title => frontMatter.Title;

    /// <inheritdoc/>
    public string? Type => frontMatter.Type;

    /// <inheritdoc/>
    public string? URL => frontMatter.URL;

    /// <inheritdoc/>
    public bool? Draft => frontMatter.Draft;

    /// <inheritdoc/>
    public List<string>? Aliases => frontMatter.Aliases;

    /// <inheritdoc/>
    public string? Section => frontMatter.Section;

    /// <inheritdoc/>
    public DateTime? Date => frontMatter.Date;

    /// <inheritdoc/>
    public DateTime? LastMod => frontMatter.LastMod;

    /// <inheritdoc/>
    public DateTime? PublishDate => frontMatter.PublishDate;

    /// <inheritdoc/>
    public DateTime? ExpiryDate => frontMatter.ExpiryDate;

    /// <inheritdoc/>
    public int Weight => frontMatter.Weight;

    /// <inheritdoc/>
    public List<string>? Tags => frontMatter.Tags;

    /// <inheritdoc/>
    public List<FrontMatterResources>? ResourceDefinitions
    {
        get => frontMatter.ResourceDefinitions;
        set => frontMatter.ResourceDefinitions = value;
    }

    /// <inheritdoc/>
    public string RawContent => frontMatter.RawContent;

    /// <inheritdoc/>
    public Kind Kind
    {
        get => frontMatter.Kind;
        set => (frontMatter as FrontMatter)!.Kind = value;
    }

    /// <inheritdoc/>
    public string? SourceRelativePath => frontMatter.SourceRelativePath;

    /// <inheritdoc/>
    public string? SourceRelativePathDirectory => frontMatter.SourceRelativePathDirectory;

    /// <inheritdoc/>
    public string SourceFullPath => frontMatter.SourceFullPath;

    /// <inheritdoc/>
    public string? SourceFullPathDirectory => frontMatter.SourceFullPathDirectory;

    /// <inheritdoc/>
    public string? SourceFileNameWithoutExtension => frontMatter.SourceFileNameWithoutExtension;

    /// <inheritdoc/>
    public Dictionary<string, object> Params
    {
        get => frontMatter.Params;
        set => frontMatter.Params = value;
    }

    #endregion IFrontMatter

    /// <summary>
    /// The source directory of the file.
    /// </summary>
    public string? SourcePathLastDirectory => string.IsNullOrEmpty(SourceRelativePathDirectory)
    ? null
    : Path.GetFileName(Path.GetFullPath(SourceRelativePathDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

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
    public BundleType BundleType { get; set; } = BundleType.none;

    /// <inheritdoc/>
    public Collection<Resource>? Resources { get; set; }

    /// <summary>
    /// Plain markdown content, without HTML.
    /// </summary>
    public string Plain => Markdown.ToPlainText(RawContent, SiteHelper.MarkdownPipeline);

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
    public bool IsPage => Kind == Kind.single;

    /// <summary>
    /// The number of words in the main content
    /// </summary>
    public int WordCount => Plain.Split(nonWords, StringSplitOptions.RemoveEmptyEntries).Length;

    private static readonly char[] nonWords = [' ', ',', ';', '.', '!', '"', '(', ')', '?', '\n', '\r'];

    /// <summary>
    /// The markdown content converted to HTML
    /// </summary>
    public string ContentPreRendered => contentPreRenderedCached.Value;

    /// <summary>
    /// The processed content.
    /// </summary>
    public string Content
    {
        get
        {
            contentCache = ParseAndRenderTemplate(false, "Error rendering theme template: {Error}");
            return contentCache!;
        }
    }

    /// <summary>
    /// Creates the output file by applying the theme templates to the page content.
    /// </summary>
    /// <returns>The processed output file content.</returns>
    public string CompleteContent => ParseAndRenderTemplate(true, "Error parsing theme template: {Error}");

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    public IEnumerable<IPage> Pages
    {
        get
        {
            if (pagesCached is not null)
            {
                return pagesCached;
            }

            pagesCached = [];
            foreach (var permalink in PagesReferences)
            {
                var page = Site.OutputReferences[permalink] as IPage;
                if (page is not null)
                {
                    pagesCached.Add(page);
                }
            }
            return pagesCached;
        }
    }

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public IEnumerable<IPage> RegularPages
    {
        get
        {
            regularPagesCache ??= Pages
                    .Where(page => page.Kind == Kind.single)
                    .ToList();
            return regularPagesCache;
        }
    }

    /// <summary>
    /// Get all URLs related to this content.
    /// </summary>
    public Dictionary<string, IOutput> AllOutputURLs
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

            if (Resources is not null)
            {
                foreach (var resource in Resources)
                {
                    if (resource.Permalink is not null && !urls.ContainsKey(resource.Permalink))
                    {
                        urls.Add(resource.Permalink, resource);
                    }
                }
            }

            return urls;
        }
    }


    /// <summary>
    /// The markdown content.
    /// </summary>
    private Lazy<string> contentPreRenderedCached => new(() => Markdown.ToHtml(RawContent, SiteHelper.MarkdownPipeline));

    /// <summary>
    /// The cached content.
    /// </summary>
    private string? contentCache { get; set; }

    private const string urlForIndex = @"{%- liquid 
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
    private const string urlForNonIndex = @"{%- liquid 
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

    private List<IPage>? regularPagesCache;

    private List<IPage>? pagesCached { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public Page(in IFrontMatter frontMatter, in ISite site)
    {
        this.frontMatter = frontMatter;
        Site = site;
    }

    /// <summary>
    /// Gets the Permalink path for the file.
    /// </summary>
    /// <param name="URLforce">The URL to consider. If null use the predefined URL</param>
    /// <returns>The output path.</returns>
    /// <see cref="urlForIndex"/>
    /// <see cref="urlForNonIndex"/>
    public string CreatePermalink(string? URLforce = null)
    {
        var isIndex = SourceFileNameWithoutExtension == "index";

        var permaLink = string.Empty;

        URLforce ??= URL ?? (isIndex ? urlForIndex : urlForNonIndex);

        try
        {
            if (Site.FluidParser.TryParse(URLforce, out var template, out var error))
            {
                var context = new TemplateContext(Site.TemplateOptions)
                    .SetValue("page", this)
                    .SetValue("site", Site);
                permaLink = template.Render(context);
            }
            else
            {
                throw new FormatException(error);
            }
        }
        catch (Exception ex)
        {
            Site.Logger.Error(ex, "Error converting URL: {URLforce}", URLforce);
        }

        if (!Path.IsPathRooted(permaLink) && !permaLink.StartsWith('/'))
        {
            permaLink = $"/{permaLink}";
        }

        return Urlizer.UrlizePath(permaLink);
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

        // Create tag pages, if any
        if (Tags is not null)
        {
            foreach (var tagName in Tags)
            {
                _ = Site.CreateSystemPage(Path.Combine("tags", tagName), tagName, "tags", this);
            }
        }

        ScanForResources();
    }

    private int counterInternal;
    private bool counterInternalLock;
    private int counter
    {
        get
        {
            if (!counterInternalLock)
            {
                counterInternalLock = true;
            }
            return counterInternal;
        }
    }

    private void ScanForResources()
    {
        if (string.IsNullOrEmpty(SourceFullPathDirectory)) return;
        if (BundleType == BundleType.none) return;

        try
        {
            var resourceFiles = Directory.GetFiles(SourceFullPathDirectory)
                .Where(file =>
                    file != SourceFullPath &&
                    (BundleType == BundleType.leaf || !file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    );

            foreach (var resourceFilename in resourceFiles)
            {
                Resources ??= [];
                var filenameOriginal = Path.GetFileName(resourceFilename);
                var filename = filenameOriginal;
                var extention = Path.GetExtension(resourceFilename);
                var title = filename;
                Dictionary<string, object> resourceParams = [];

                if (ResourceDefinitions is not null)
                {
                    if (counterInternalLock)
                    {
                        counterInternalLock = false;
                        ++counterInternal;
                    }
                    foreach (var resourceDefinition in ResourceDefinitions)
                    {
                        resourceDefinition.GlobMatcher ??= new();
                        _ = resourceDefinition.GlobMatcher.AddInclude(resourceDefinition.Src);
                        var file = new InMemoryDirectoryInfo("./", new[] { filenameOriginal });
                        if (resourceDefinition.GlobMatcher.Execute(file).HasMatches)
                        {
                            if (Site.FluidParser.TryParse(resourceDefinition.Name, out var templateFileName, out var errorFileName))
                            {
                                var context = new TemplateContext(Site.TemplateOptions)
                                    .SetValue("page", this)
                                    .SetValue("site", Site)
                                    .SetValue("counter", counter);
                                filename = templateFileName.Render(context);
                            }
                            if (Site.FluidParser.TryParse(resourceDefinition.Title, out var templateTitle, out var errorTitle))
                            {
                                var context = new TemplateContext(Site.TemplateOptions)
                                    .SetValue("page", this)
                                    .SetValue("site", Site)
                                    .SetValue("counter", counter);
                                title = templateTitle.Render(context);
                            }
                            resourceParams = resourceDefinition.Params ?? [];
                        }
                    }
                }

                filename = Path.GetFileNameWithoutExtension(filename) + extention;
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
            return;
        }
    }

    private string ParseAndRenderTemplate(bool isBaseTemplate, string errorMessage)
    {
        var fileContents = FileUtils.GetTemplate(Site.SourceThemePath, this, Site.CacheManager, isBaseTemplate);
        if (string.IsNullOrEmpty(fileContents))
        {
            return isBaseTemplate ? Content : ContentPreRendered;
        }

        if (Site.FluidParser.TryParse(fileContents, out var template, out var error))
        {
            var context = new TemplateContext(Site.TemplateOptions)
                .SetValue("page", this)
                .SetValue("site", Site);
            try
            {
                var rendered = template.Render(context);
                return rendered;
            }
            catch (Exception ex)
            {
                Site.Logger.Error(ex, errorMessage, error);
                return string.Empty;
            }
        }

        Site.Logger.Error(errorMessage, error);
        return string.Empty;
    }
}
