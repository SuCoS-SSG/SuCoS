// ReSharper disable InconsistentNaming
namespace SuCoS.Models;

/// <summary>
/// The type of the output page, if it's a single page, a list of pages or the home page.
/// </summary>
[Flags]
public enum Kind
{
    /// <summary>
    /// A single content page.
    /// </summary>
    single = 1 << 1,

    /// <summary>
    /// List of contents
    /// </summary>
    list = 1 << 2,

    /// <summary>
    /// Special page, like the home page. It will be rendered as index.html.
    /// </summary>
    index = 1 << 3,

    /// <summary>
    /// Created by system
    /// </summary>
    system = 1 << 4,

    /// <summary>
    /// Taxonomy type of content
    /// </summary>
    istaxonomy = 1 << 5,

    /// <summary>
    /// Special page, like the home page. It will be rendered as index.html.
    /// </summary>
    home = system | index | list,

    /// <summary>
    /// Root content list type
    /// </summary>
    section = system | list,

    /// <summary>
    /// Taxonomy group is equivalent to Section
    /// </summary>
    taxonomy = system | istaxonomy | list,

    /// <summary>
    /// Each taxonomy (category, tags) item
    /// </summary>
    term = system | single | istaxonomy | list,
}
