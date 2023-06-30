namespace SuCoS.Models;

/// <summary>
/// A scafold structure to help creating system-generated content, like
/// tag, section or index pages
/// </summary>
public class BasicContent : IBaseContent
{
    /// <inheritdoc/>
    public string Title { get; }

    /// <inheritdoc/>
    public string Section { get; }

    /// <inheritdoc/>
    public Kind Kind { get; }

    /// <inheritdoc/>
    public string Type { get; }

    /// <inheritdoc/>
    public string URL { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="title"></param>
    /// <param name="section"></param>
    /// <param name="type"></param>
    /// <param name="url"></param>
    /// <param name="kind"></param>
    public BasicContent(string title, string section, string type, string url, Kind kind = Kind.list)
    {
        Title = title;
        Section = section;
        Kind = kind;
        Type = type;
        URL = url;
    }
}
