namespace SuCoS.Models;

/// <summary>
/// The type of content bundle.
/// </summary>
public enum BundleType
{
    /// <summary>
    /// Regular page. Not a bundle.
    /// </summary>
    none,

    /// <summary>
    /// Bundle with no children
    /// </summary>
    leaf,

    /// <summary>
    /// Bundle with children embedded, like a home page, taxonomy term, taxonomy list
    /// </summary>
    branch
}
