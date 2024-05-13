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
    Single = 1 << 1,

    /// <summary>
    /// List of contents
    /// </summary>
    List = 1 << 2,

    /// <summary>
    /// Special page, like the home page. It will be rendered as index.html.
    /// </summary>
    Index = 1 << 3,

    /// <summary>
    /// Created by system
    /// </summary>
    System = 1 << 4,

    /// <summary>
    /// Taxonomy type of content
    /// </summary>
    IsTaxonomy = 1 << 5,

    /// <summary>
    /// Special page, like the home page. It will be rendered as index.html.
    /// </summary>
    Home = System | Index,

    /// <summary>
    /// Root content list type
    /// </summary>
    Section = System | List,

    /// <summary>
    /// Taxonomy group is equivalent to Section
    /// </summary>
    Taxonomy = System | IsTaxonomy | List,

    /// <summary>
    /// Each taxonomy (category, tags) item
    /// </summary>
    Term = System | Single | IsTaxonomy | List,
}
