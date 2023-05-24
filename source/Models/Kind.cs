namespace SuCoS.Models;

/// <summary>
/// The type of the output page, if it's a single page, a list of pages or the home page.
/// </summary>
public enum Kind
{
    /// <summary>
    /// A single content page.
    /// </summary>
    single,

    /// <summary>
    /// List of contents
    /// </summary>
    list,

    /// <summary>
    /// Special page, like the home page. It will be rendered as index.html.
    /// </summary>
    index
}
