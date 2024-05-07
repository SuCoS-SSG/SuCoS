namespace SuCoS.Models;

/// <summary>
/// The type of content bundle.
/// </summary>
public enum BundleType
{
    /// <summary>
    /// Regular page. Not a bundle.
    /// </summary>
    None,

    /// <summary>
    /// Bundle with no children
    /// </summary>
    Leaf,

    /// <summary>
    /// Bundle with children embedded, like a home page, taxonomy term, taxonomy list
    /// </summary>
    Branch
}
