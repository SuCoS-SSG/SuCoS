using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Markdig;
using Microsoft.Extensions.FileSystemGlobbing;
using SuCoS.Helpers;

namespace SuCoS.Models;

/// <summary>
/// Each page data created from source files or from the system.
/// </summary>
public class Page : IPage, IContentSource, IFrontMatter, IOutput
{
    #region IPage

    /// <inheritdoc/>
    public ContentSource ContentSource { get; init; }

    /// <inheritdoc/>
    public string? SourcePathLastDirectory =>
        string.IsNullOrEmpty(SourceRelativePathDirectory)
            ? null
            : Path.GetFileName(Path.GetFullPath(
                SourceRelativePathDirectory.TrimEnd(Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)));

    /// <inheritdoc/>
    public ISite Site { get; }

    /// <inheritdoc/>
    public Collection<string>? AliasesProcessed { get; private set; }

    /// <inheritdoc/>
    public ConcurrentBag<string> PagesReferences { get; } = [];

    /// <inheritdoc/>
    public IPage? Parent => ContentSourceParent?.ContentSourceToPages.Count > 0
        ? ContentSourceParent.ContentSourceToPages[0]
        : null;

    /// <inheritdoc/>
    public string Plain =>
        Markdown.ToPlainText(RawContent, SiteHelper.MarkdownPipeline);

    /// <inheritdoc/>
    // TODO:
    public List<IPage> TagsReference
    {
        get
        {
            List<IPage> tagsReferences = [];
            foreach (var tag in ContentSourceTags)
            {
                tagsReferences.AddRange(tag.ContentSourceToPages
                    .Where(page => page.OutputFormat == OutputFormat));
            }

            return tagsReferences;
        }
    }

    /// <inheritdoc/>
    public bool IsHome => Site.Home == this;

    /// <inheritdoc/>
    public bool IsPage => Kind == Kind.single;

    /// <inheritdoc/>
    public bool IsSection => Type == "section";

    /// <inheritdoc/>
    public int WordCount => Plain
        .Split(IPage.NonWords,
            StringSplitOptions.RemoveEmptyEntries).Length;

    /// <inheritdoc/>
    public string ContentPreRendered => ContentPreRenderedCached.Value;

    /// <inheritdoc/>
    public string Content => ParseAndRenderTemplate(false);

    /// <inheritdoc/>
    public string CompleteContent => ParseAndRenderTemplate(true);

    /// <inheritdoc/>
    public string OutputFormat { get; set; }

    /// <inheritdoc/>
    public List<string> OutputFormats { get; set; }

    /// <inheritdoc/>
    public IEnumerable<IPage> Pages
    {
        get
        {
            _pages ??= ContentSource.Children
                .SelectMany(page => page.ContentSourceToPages)
                .Where(page => page.OutputFormat == OutputFormat)
                .ToList() ?? [];
            return _pages;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IPage> RegularPages
    {
        get
        {
            _regularPages ??= Pages
                .Where(page => page.IsPage);
            return _regularPages;
        }
    }

    /// <inheritdoc/>
    public Dictionary<string, IOutput> AllOutputUrLs
    {
        get
        {
            var urls = new Dictionary<string, IOutput>();

            if (!string.IsNullOrEmpty(RelPermalink))
            {
                urls.Add(RelPermalink, this);
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
                if (resource.RelPermalink is not null)
                {
                    urls.TryAdd(resource.RelPermalink, resource);
                }
            }

            return urls;
        }
    }

    /// <inheritdoc/>
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

        if (!permalink.StartsWith('/'))
        // if (!Path.IsPathRooted(permalink) && !permalink.StartsWith('/'))
        {
            permalink = $"/{permalink}";
        }

        if ((this as IFile).SourceFullPathDirectory(Site.SourceContentPath) ==
            "/")
        {
            permalink = "/";
        }

        var useUgly = (!OutputFormatObj.NoUgly &&
                       (OutputFormatObj.Ugly || Site.UglyUrLs));

        var relPermalinkDir = Urlizer.UrlizePath(permalink);
        relPermalinkDir = (relPermalinkDir.EndsWith('/')
            ? relPermalinkDir
            : relPermalinkDir + "/");
        var relPermalinkFilename = Urlizer.UrlizePath(
            useUgly
                ? $"{SourceFileNameWithoutExtension}.{OutputFormatObj.Extension}"
                : $"{OutputFormatObj.BaseName}.{OutputFormatObj.Extension}");

        var urlFinal = relPermalinkDir + relPermalinkFilename;

        return urlFinal;
    }

    /// <inheritdoc/>
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

        PostProcessResources();
    }


    #endregion IPage

    #region IFrontMatter

    /// <inheritdoc/>
    public string? Title => ContentSource.Title;

    /// <inheritdoc/>
    public string? Section => ContentSource.Section;

    /// <inheritdoc/>
    public string? Url => ContentSource.Url;

    /// <inheritdoc/>
    public bool? Draft => ContentSource.Draft;

    /// <inheritdoc/>
    public List<string>? Aliases => ContentSource.Aliases;

    /// <inheritdoc/>
    public DateTime? Date => ContentSource.Date;

    /// <inheritdoc/>
    public DateTime? LastMod => ContentSource.LastMod;

    /// <inheritdoc/>
    public DateTime? PublishDate => ContentSource.PublishDate;

    /// <inheritdoc/>
    public DateTime? ExpiryDate => ContentSource.ExpiryDate;

    /// <inheritdoc/>
    public int Weight => ContentSource.Weight;

    /// <inheritdoc/>
    public List<string>? Tags => ContentSource.Tags;

    /// <inheritdoc/>
    public List<FrontMatterResources>? ResourceDefinitions =>
        ContentSource.ResourceDefinitions;

    /// <inheritdoc/>
    public string RawContent => ContentSource.RawContent;

    /// <inheritdoc/>
    public List<IPage> ContentSourceToPages =>
        ContentSource.ContentSourceToPages;

    /// <inheritdoc/>
    public ContentSource? ContentSourceParent =>
        ContentSource.ContentSourceParent;

    /// <inheritdoc/>
    public string SourceRelativePath => ContentSource.SourceRelativePath;

    /// <inheritdoc/>
    public string? SourceRelativePathDirectory =>
        ContentSource.SourceRelativePathDirectory;

    /// <inheritdoc/>
    public string? SourceFileNameWithoutExtension =>
        (ContentSource as IFile).SourceFileNameWithoutExtension;

    #endregion IFrontMatter

    #region IContentSource

    /// <inheritdoc/>
    public string? Type => ContentSource.Type;

    /// <inheritdoc/>
    public Kind Kind => ContentSource.Kind;

    /// <inheritdoc/>
    public BundleType BundleType => ContentSource.BundleType;

    /// <inheritdoc/>
    public List<ContentSource> ContentSourceTags =>
        ContentSource.ContentSourceTags;

    #endregion IContentSource

    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params
    {
        get => ContentSource.Params;
        set => ContentSource.Params = value;
    }

    #endregion IParams

    #region IOutput

    /// <inheritdoc/>
    public string RelPermalink { get; set; } = string.Empty;

    #endregion IOutput

    /// <summary>
    /// List of attached resources
    /// </summary>
    // TODO: why is this public?
    public List<Resource>? Resources { get; set; }

    /// <summary>
    /// The actual object with OutputFormat data
    /// </summary>
    // TODO: why is this public?
    public OutputFormat OutputFormatObj { get; }

    /// <summary>
    /// The markdown content.
    /// </summary>
    private Lazy<string> ContentPreRenderedCached => new(() =>
        Markdown.ToHtml(RawContent, SiteHelper.MarkdownPipeline));

    private const string UrlForIndex = @"{%- liquid
if page.Parent
echo page.Parent.RelPermalinkDir
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
echo page.Parent.RelPermalinkDir
echo '/'
endif
if page.Title != ''
echo page.Title
else
echo page.SourceFileNameWithoutExtension
endif
-%}";

    private IEnumerable<IPage>? _regularPages;

    private List<IPage>? _pages;

    /// <summary>
    /// Constructor
    /// </summary>
    public Page(in ContentSource contentSource, in ISite site,
        string outputFormat, List<string> outputFormats)
    {
        ContentSource = contentSource;
        Site = site;
        OutputFormat = outputFormat;
        OutputFormats = outputFormats;

        FileUtils.OutputFormats.TryGetValue(OutputFormat,
            out var outputFormatObj);
        if (outputFormatObj is null)
        {
            throw new ArgumentException("No output format for {OutputFormat}",
                OutputFormat);
        }

        OutputFormatObj = outputFormatObj;
    }

    private string ParseAndRenderTemplate(bool isBaseTemplate)
    {
        var fileContents = this.GetTemplate(Site.SourceThemePath,
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
            Site.Logger.Error(ex,
                "Error rendering theme template: {fileContents}",
                fileContents);
            return string.Empty;
        }
    }

    /// <summary>
    /// Process resources for this page, generating permalinks
    /// </summary>
    private void PostProcessResources()
    {
        if (ContentSource.RawResources?.Any() != true)
        {
            return;
        }

        Resources = ProcessResourcesWithDefinitions().ToList();
    }

    private IEnumerable<Resource> ProcessResourcesWithDefinitions()
    {
        var counter = 0;
        return ContentSource.RawResources!
            .Where(resource => resource.Resource == null)
            .Select(sourceResource => CreateResourceWithCustomization(sourceResource, ref counter))
            .Where(resource => resource != null)!;
    }

    private Resource? CreateResourceWithCustomization(ContentSourceResource sourceResource, ref int counter)
    {
        var filenameOriginal = Path.GetFileName(sourceResource.SourceRelativePath);
        var extension = Path.GetExtension(sourceResource.SourceRelativePath);

        var resourceCustomization = GetResourceCustomization(filenameOriginal, ref counter);

        var filename = resourceCustomization.Filename ?? filenameOriginal;
        filename = Path.GetFileNameWithoutExtension(filename) + extension;

        var resource = new Resource
        {
            Title = resourceCustomization.Title ?? filenameOriginal,
            Params = resourceCustomization.Params ?? sourceResource.Params,
            SourceRelativePath = sourceResource.SourceRelativePath,
            Site = Site,
            RelPermalink = Path.Combine((this as IOutput).RelPermalinkDir, filename)
        };
        sourceResource.Resource = resource;
        return resource;
    }

    private (string? Filename, string? Title, Dictionary<string, object>? Params) GetResourceCustomization(string filenameOriginal, ref int counter)
    {
        // Early return if no resource definitions
        if (ResourceDefinitions == null)
        {
            return (null, null, null);
        }

        // Find first matching resource definition
        var matchedDefinition = ResourceDefinitions
            .FirstOrDefault(resourceDefinition =>
            {
                resourceDefinition.GlobMatcher ??= new();
                _ = resourceDefinition.GlobMatcher.AddInclude(resourceDefinition.Src);

                var file = new InMemoryDirectoryInfo("./", new[] { filenameOriginal });
                return resourceDefinition.GlobMatcher.Execute(file).HasMatches;
            });

        // If no match found, return null
        if (matchedDefinition == null)
        {
            return (null, null, null);
        }

        // Process matched definition
        var filename = Site.TemplateEngine.ParseResource(
            matchedDefinition.Name, Site, this, counter) ?? filenameOriginal;

        var title = Site.TemplateEngine.ParseResource(
            matchedDefinition.Title, Site, this, counter) ?? filenameOriginal;

        counter++;

        return (filename, title, matchedDefinition.Params);
    }
}
