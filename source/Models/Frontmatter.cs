using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fluid;
using Markdig;
using SuCoS.Helpers;
using YamlDotNet.Serialization;

namespace SuCoS.Models;

/// <summary>
/// The meta data about each content Markdown file.
/// </summary>
public class Frontmatter : IBaseContent, IParams
{
    #region IBaseContent

    /// <inheritdoc/>
    public string? Title { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Section { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Kind Kind { get; set; } = Kind.single;

    /// <inheritdoc/>
    public string? Type { get; set; } = "page";

    /// <inheritdoc/>
    public string? URL { get; init; }

    #endregion IBaseContent

    #region IParams

    /// <inheritdoc/>
    [YamlIgnore]
    public Dictionary<string, object> Params { get; set; } = new();

    #endregion IParams

    /// <summary>
    /// Gets or sets the date of the page.
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Gets or sets the last modification date of the page.
    /// </summary>
    public DateTime? LastMod { get; set; }

    /// <summary>
    /// Gets or sets the publish date of the page.
    /// </summary>
    public DateTime? PublishDate { get; set; }

    /// <summary>
    /// Gets or sets the expiry date of the page.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// The path of the file, if it's a file.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Secondary URL patterns to be used to create the url.
    /// </summary>
    public List<string>? Aliases { get; set; }

    /// <summary>
    /// Page weight. Useful for sorting.
    /// </summary>
    public int Weight { get; set; } = 0;

    /// <summary>
    /// The source filename, without the extension. ;)
    /// </summary>
    [YamlIgnore]
    public string? SourceFileNameWithoutExtension { get; set; }

    /// <summary>
    /// The source directory of the file.
    /// </summary>
    [YamlIgnore]
    public string? SourcePathDirectory { get; set; }

    /// <summary>
    /// The source directory of the file.
    /// </summary>
    [YamlIgnore]
    public string? SourcePathLastDirectory => Path.GetDirectoryName(SourcePathDirectory ?? string.Empty);

    /// <summary>
    /// Point to the site configuration.
    /// </summary>
    [YamlIgnore]
    public Site Site { get; set; }

    /// <summary>
    /// Secondary URL patterns to be used to create the url.
    /// </summary>
    [YamlIgnore]
    public List<string>? AliasesProcessed { get; set; }

    /// <summary>
    /// The URL for the content.
    /// </summary>
    [YamlIgnore]
    public string? Permalink { get; set; }

    /// <summary>
    /// Raw content, from the Markdown file.
    /// </summary>
    [YamlIgnore]
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    [YamlIgnore]
    public ConcurrentBag<string>? PagesReferences { get; set; }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    [YamlIgnore]
    public Frontmatter? Parent { get; set; }

    /// <summary>
    /// A list of tags, if any.
    /// </summary>
    [YamlIgnore]
    public List<Frontmatter>? Tags { get; set; }

    /// <summary>
    /// Check if the page is expired
    /// </summary>
    public bool IsDateExpired => ExpiryDate is not null && ExpiryDate <= clock.Now;

    /// <summary>
    /// Check if the page is publishable
    /// </summary>
    [YamlIgnore]
    public bool IsDatePublishable => GetPublishDate is null || GetPublishDate <= clock.Now;

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
    public bool IsPage => Type == "page";

    /// <summary>
    /// The markdown content converted to HTML
    /// </summary>
    public string ContentPreRendered
    {
        get
        {
            contentPreRenderedCached ??= Markdown.ToHtml(RawContent, Site.MarkdownPipeline);
            return contentPreRenderedCached;
        }
    }

    /// <summary>
    /// The processed content.
    /// </summary>
    public string Content
    {
        get
        {
            if (contentCacheTime is not null && !(Site.IgnoreCacheBefore > contentCacheTime))
            {
                return contentCache!;
            }
            contentCache = CreateContent();
            contentCacheTime = clock.UtcNow;
            return contentCache!;
        }
    }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    public IEnumerable<Frontmatter> Pages
    {
        get
        {
            if (PagesReferences is null)
            {
                return new List<Frontmatter>();
            }

            if (pagesCached is not null)
            {
                return pagesCached;
            }

            pagesCached ??= new();
            foreach (var permalink in PagesReferences)
            {
                pagesCached.Add(Site.PagesDict[permalink]);
            }
            return pagesCached;
        }
    }

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public IEnumerable<Frontmatter> RegularPages
    {
        get
        {
            regularPagesCache ??= Pages
                    .Where(frontmatter => frontmatter.Kind == Kind.single)
                    .ToList();
            return regularPagesCache;
        }
    }

    /// <summary>
    /// Get all URLs related to this content.
    /// </summary>
    public List<string> Urls
    {
        get
        {
            var urls = new List<string>();
            if (Permalink is not null)
            {
                urls.Add(Permalink);
            }

            if (AliasesProcessed is not null)
            {
                urls.AddRange(from aliases in AliasesProcessed
                              select aliases);
            }

            return urls;
        }
    }

    /// <summary>
    /// The markdown content.
    /// </summary>
    private string? contentPreRenderedCached { get; set; }

    /// <summary>
    /// The cached content.
    /// </summary>
    private string? contentCache { get; set; }

    /// <summary>
    /// The time when the content was cached.
    /// </summary>
    private DateTime? contentCacheTime { get; set; }

    private List<Frontmatter>? regularPagesCache;

    private List<Frontmatter>? pagesCached { get; set; }

    private DateTime? GetPublishDate => PublishDate ?? Date;

    private ISystemClock clock => Site.Clock;

    /// <summary>
    /// Required.
    /// </summary>
    public Frontmatter(
        string title,
        string sourcePath,
        Site site,
        string? sourceFileNameWithoutExtension = null,
        string? sourcePathDirectory = null)
    {
        Title = title;
        Site = site;
        SourcePath = sourcePath;
        SourceFileNameWithoutExtension = sourceFileNameWithoutExtension ?? Path.GetFileNameWithoutExtension(sourcePath);
        SourcePathDirectory = sourcePathDirectory ?? Path.GetDirectoryName(sourcePath) ?? string.Empty;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public Frontmatter()
    {
    }

    /// <summary>
    /// Check if the page have a publishing date from the past.
    /// </summary>
    /// <param name="options">options</param>
    /// <returns></returns>
    public bool IsValidDate(IGenerateOptions? options)
    {
        return !IsDateExpired && (IsDatePublishable || (options?.Future ?? false));
    }

    /// <summary>
    /// Gets the Permalink path for the file.
    /// </summary>
    /// <param name="URLforce">The URL to consider. If null, we get frontmatter.URL</param>
    /// <returns>The output path.</returns>
    public string CreatePermalink(string? URLforce = null)
    {
        var isIndex = SourceFileNameWithoutExtension == "index";

        var permaLink = string.Empty;

        URLforce ??= URL
            ?? (isIndex 
            ? @"{%- liquid 
if page.Parent
echo page.Parent.Permalink
echo '/'
endif
if page.Title != ''
echo page.Title
else
echo page.SourcePathLastDirectory
endif
-%}" 
            : @"{%- liquid 
if page.Parent
echo page.Parent.Permalink
echo '/'
endif
if page.Title != ''
echo page.Title
else
echo page.SourceFileNameWithoutExtension
endif
-%}");

        try
        {
            if (Site.FluidParser.TryParse(URLforce, out var template, out var error))
            {
                var context = new TemplateContext(Site.TemplateOptions)
                    .SetValue("page", this);
                permaLink = template.Render(context);
            }
            else
            {
                throw new FormatException(error);
            }
        }
        catch (Exception ex)
        {
            Site.Logger?.Error(ex, "Error converting URL: {URLforce}", URLforce);
        }

        if (!Path.IsPathRooted(permaLink) && !permaLink.StartsWith('/'))
        {
            permaLink = '/' + permaLink;
        }

        return Urlizer.UrlizePath(permaLink);
    }

    /// <summary>
    /// Creates the output file by applying the theme templates to the frontmatter content.
    /// </summary>
    /// <returns>The processed output file content.</returns>
    public string CreateOutputFile()
    {
        // Process the theme base template
        // If the theme base template file is available, parse and render the template using the frontmatter data
        // Otherwise, use the processed content as the final result
        // Any error during parsing is logged, and an empty string is returned
        // The final result is stored in the 'result' variable and returned
        var fileContents = FileUtils.GetTemplate(Site.SourceThemePath, this, Site, true);
        if (string.IsNullOrEmpty(fileContents))
        {
            return Content;
        }

        if (Site.FluidParser.TryParse(fileContents, out var template, out var error))
        {
            var context = new TemplateContext(Site.TemplateOptions);
            _ = context.SetValue("page", this);
            return template.Render(context);
        }

        Site.Logger?.Error("Error parsing theme template: {Error}", error);
        return string.Empty;
    }

    /// <summary>
    /// Create the page content, with converted Markdown and themed.
    /// </summary>
    /// <returns></returns>
    private string CreateContent()
    {
        var fileContents = FileUtils.GetTemplate(Site.SourceThemePath, this, Site);
        // Theme content
        if (string.IsNullOrEmpty(fileContents))
        {
            return ContentPreRendered;
        }

        if (Site.FluidParser.TryParse(fileContents, out var template, out var error))
        {
            var context = new TemplateContext(Site.TemplateOptions)
                .SetValue("page", this);
            try
            {
                var rendered = template.Render(context);
                return rendered;
            }
            catch (Exception ex)
            {
                Site.Logger?.Error(ex, "Error rendering theme template: {Error}", error);
                return string.Empty;
            }
        }

        Site.Logger?.Error("Error parsing theme template: {Error}", error);
        return string.Empty;

    }
}
