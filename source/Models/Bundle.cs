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
    /// Bundle with no childre
    /// </summary>
    leaf,

    /// <summary>
    /// Bundle with children embeded, like a home page, taxonomy term, taxonomy list
    /// </summary>
    branch
}
