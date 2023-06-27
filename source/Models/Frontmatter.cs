using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fluid;
using Markdig;
using Serilog;
using SuCoS.Models;

namespace SuCoS;

/// <summary>
/// The meta data about each content Markdown file.
/// </summary>
public class Frontmatter : IBaseContent, IParams
{
    #region IBaseContent

    /// <inheritdoc/>
    public string Title { get; init; }

    /// <inheritdoc/>
    public string Section { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Kind Kind { get; set; } = Kind.single;

    /// <inheritdoc/>
    public string Type { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? URL { get; set; }

    #endregion IBaseContent

    #region IParams

    /// <inheritdoc/>
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
    public string SourcePath { get; init; }

    /// <summary>
    /// The source filename, without the extension. ;)
    /// </summary>
    public string SourceFileNameWithoutExtension { get; init; }

    /// <summary>
    /// The source directory of the file.
    /// </summary>
    public string SourcePathDirectory { get; init; }

    /// <summary>
    /// Point to the site configuration.
    /// </summary>
    public Site Site { get; init; }

    /// <summary>
    /// Secondary URL patterns to be used to create the url.
    /// </summary>
    public List<string>? Aliases { get; set; }

    /// <summary>
    /// The URL for the content.
    /// </summary>
    public string? Permalink { get; set; }

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
            if (Aliases is not null)
            {
                foreach (var aliases in Aliases)
                {
                    urls.Add(aliases);
                }
            }
            return urls;
        }
    }

    /// <summary>
    /// Raw content, from the Markdown file.
    /// </summary>
    public string RawContent { get; set; } = string.Empty;

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
            if (contentCacheTime is null || Site.IgnoreCacheBefore >= contentCacheTime)
            {
                contentCache = CreateContent();
                contentCacheTime = DateTime.UtcNow;
            }
            return contentCache!;
        }
    }

    /// <summary>
    /// A list of tags, if any.
    /// </summary>
    public List<Frontmatter>? Tags { get; set; }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    public List<Frontmatter> Pages
    {
        get
        {
            if (PagesReferences is null)
            {
                return new List<Frontmatter>();
            }

            if (pagesCached is null)
            {
                pagesCached ??= new();
                foreach (var permalink in PagesReferences)
                {
                    pagesCached.Add(Site.PagesDict[permalink]);
                }
            }
            return pagesCached;
        }
    }

    /// <summary>
    /// Other content that mention this content.
    /// Used to create the tags list and Related Posts section.
    /// </summary>
    public ConcurrentBag<string>? PagesReferences { get; set; }

    /// <summary>
    /// List of pages from the content folder.
    /// </summary>
    public List<Frontmatter> RegularPages
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
    /// Language of the content.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Check if the page is expired
    /// </summary>
    public bool IsDateExpired => ExpiryDate is not null && ExpiryDate >= DateTime.Now;

    /// <summary>
    /// Check if the page is publishable
    /// </summary>
    public bool IsDatePublishable => GetPublishDate is null || GetPublishDate <= DateTime.Now;

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
        string outputRelativePath;

        URLforce ??= URL
            ?? (isIndex ? "{{ page.SourcePathDirectory }}" : "{{ page.SourcePathDirectory }}/{{ page.Title }}");

        outputRelativePath = URLforce;

        if (Site.FluidParser.TryParse(URLforce, out var template, out var error))
        {
            var context = new TemplateContext(Site.TemplateOptions)
                .SetValue("page", this);
            try
            {
                outputRelativePath = template.Render(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error converting URL: {Error}", error);
            }
        }

        outputRelativePath = Urlizer.UrlizePath(outputRelativePath);

        if (!Path.IsPathRooted(outputRelativePath) && !outputRelativePath.StartsWith("/"))
        {
            outputRelativePath = "/" + outputRelativePath;
        }

        return outputRelativePath;
    }

    /// <summary>
    /// Creates the output file by applying the theme templates to the frontmatter content.
    /// </summary>
    /// <returns>The processed output file content.</returns>
    public string CreateOutputFile()
    {
        // Theme content
        string result;

        // Process the theme base template
        // If the theme base template file is available, parse and render the template using the frontmatter data
        // Otherwise, use the processed content as the final result
        // Any error during parsing is logged, and an empty string is returned
        // The final result is stored in the 'result' variable and returned
        var fileContents = FileUtils.GetTemplate(Site.SourceThemePath, this, Site, true);
        if (string.IsNullOrEmpty(fileContents))
        {
            result = Content;
        }
        else if (Site.FluidParser.TryParse(fileContents, out var template, out var error))
        {
            var context = new TemplateContext(Site.TemplateOptions);
            _ = context.SetValue("page", this);
            result = template.Render(context);
        }
        else
        {
            Log.Error("Error parsing theme template: {Error}", error);
            return string.Empty;
        }

        return result;
    }

    /// <summary>
    /// Create the page content, with converted Markdown and themed.
    /// </summary>
    /// <returns></returns>
    private string CreateContent()
    {
        var fileContents = FileUtils.GetTemplate(Site.SourceThemePath, this, Site);
        // Theme content
        if (string.IsNullOrEmpty(value: fileContents))
        {
            return Content;
        }
        else if (Site.FluidParser.TryParse(fileContents, out var template, out var error))
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
                Log.Error(ex, "Error rendering theme template: {Error}", error);
                return string.Empty;
            }
        }
        else
        {
            Log.Error("Error parsing theme template: {Error}", error);
            return string.Empty;
        }
    }
}
