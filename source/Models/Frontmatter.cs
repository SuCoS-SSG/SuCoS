using System.Collections.Generic;

namespace SuCoS;

/// <summary>
/// The meta data about each content Markdown file.
/// </summary>
public class Frontmatter
{
    /// <summary>
    /// The content Title.
    /// </summary>
    public string? Title { get; set; } = "";

    /// <summary>
    /// The URL pattern to be used to create the url;
    /// </summary>
    public string? URL { get; set; }

    /// <summary>
    /// The URL for the content;
    /// </summary>
    public string? Permalink { get; set; }

    /// <summary>
    /// Ray content.
    /// </summary>
    public string ContentRaw { get; set; } = "";

    /// <summary>
    /// The raw content, extracted from each Markdown file.
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// A list of tags, if any.
    /// </summary>
    public List<string>? Tags { get; set; }
}
