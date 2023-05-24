using SuCoS.Models;

namespace SuCoS;

/// <summary>
/// Responsible for creating the taxonomy pages.
/// </summary>
public interface ITaxonomyCreator
{
    /// <summary>
    /// Creates a tag page with the given name.
    /// </summary>
    /// <param name="site"></param>
    /// <param name="tagName"></param>
    /// <param name="originalFrontmatter"></param>
    Frontmatter CreateTagFrontmatter(Site site, string tagName, Frontmatter originalFrontmatter);
}
