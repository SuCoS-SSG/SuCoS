namespace SuCoS.Models;

/// <summary>
/// Basic structure needed to generate user content and system content
/// </summary>
public interface IBaseContent
{
    /// <summary>
    /// The content Title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The directory where the content is located.
    /// </summary>
    /// 
    /// <example>
    /// If the content is located at <c>content/blog/2021-01-01-Hello-World.md</c>, 
    /// then the value of this property will be <c>blog</c>.
    /// </example>
    string Section { get; }

    /// <summary>
    /// The type of the page, if it's a single page, a list of pages or the home page.
    /// </summary>
    Kind Kind { get; }

    /// <summary>
    /// The type of content. It's the same of the Section, if not specified.
    /// </summary>
    string Type { get; }

    /// <summary>
    /// The URL pattern to be used to create the url.
    /// </summary>
    string? URL { get; }
}
