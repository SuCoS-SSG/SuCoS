namespace SuCoS.Models;

/// <summary>
/// Represents an output format.
/// </summary>
public class OutputFormat
{
    /// <summary>
    /// The base name of the published file.
    /// </summary>
    public string BaseName { get; init; } = "index";

    /// <summary>
    /// File extension of the output format.
    /// </summary>
    public string Extension { get; init; } = "html";

    /// <summary>
    /// The media type of the published file. This must match a defined media type, either built-in or custom.
    /// </summary>
    public bool MediaType { get; init; }

    /// <summary>
    /// If provided, you can assign this value to rel attributes in link elements when iterating over output
    /// formats in your templates.
    /// </summary>
    public bool Rel { get; init; }

    /// <summary>
    /// If true, the Permalink and RelPermalink methods on a Page object return the rendering output format
    /// rather than main output format (see below).
    /// Enabled by default for the html and amp output formats
    /// </summary>
    public bool Permalinkable { get; init; }

    /// <summary>
    /// If true, disables ugly URLs for this output format when uglyURLs is true in your site configuration.
    /// </summary>
    public bool NoUgly { get; init; }

    /// <summary>
    /// If true, enables uglyURLs for this output format when uglyURLs is false in your site configuration.
    /// </summary>
    public bool Ugly { get; init; }
}
