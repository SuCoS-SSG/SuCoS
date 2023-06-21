using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using SuCoS.Models;

namespace SuCoS;

/// <summary>
/// The meta data about each content Markdown file.
/// </summary>
public class Frontmatter : IBaseContent, IParams
{
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
    /// The markdown content.
    /// </summary>
    private string ContentPreRenderedcached { get; set; } = string.Empty;

    /// <summary>
    /// The markdown content converted to HTML
    /// </summary>
    public string ContentPreRendered
    {
        get
        {
            if (string.IsNullOrEmpty(ContentPreRenderedcached))
            {
                ContentPreRenderedcached = BaseGeneratorCommand.CreateContentPreRendered(this);
            }
            return ContentPreRenderedcached;
        }
    }

    /// <summary>
    /// The cached content.
    /// </summary>
    private string? ContentCache { get; set; }

    /// <summary>
    /// The time when the content was cached.
    /// </summary>
    private DateTime? ContentCacheTime { get; set; }

    /// <summary>
    /// The processed content.
    /// </summary>
    public string Content
    {
        get
        {
            if (ContentCacheTime is null || BaseGeneratorCommand.IgnoreCacheBefore >= ContentCacheTime)
            {
                ContentCache = BaseGeneratorCommand.CreateContent(this);
                ContentCacheTime = DateTime.UtcNow;
            }
            return ContentCache!;
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
    public ConcurrentBag<Frontmatter>? Pages { get; set; }

    /// <summary>
    /// Language of the content.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Content generator.
    /// </summary>
    private BaseGeneratorCommand BaseGeneratorCommand { get; set; }

    /// <summary>
    /// Required.
    /// </summary>
    public Frontmatter(
        BaseGeneratorCommand baseGeneratorCommand,
        string title,
        string sourcePath,
        Site site,
        string? sourceFileNameWithoutExtension = null,
        string? sourcePathDirectory = null)
    {
        BaseGeneratorCommand = baseGeneratorCommand;
        Title = title;
        Site = site;
        SourcePath = sourcePath;
        SourceFileNameWithoutExtension = sourceFileNameWithoutExtension ?? Path.GetFileNameWithoutExtension(sourcePath);
        SourcePathDirectory = sourcePathDirectory ?? Path.GetDirectoryName(sourcePath) ?? string.Empty;
    }
}
