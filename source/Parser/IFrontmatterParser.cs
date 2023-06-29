using SuCoS.Models;

namespace SuCoS.Parser;

/// <summary>
/// Responsible for parsing the content frontmatter
/// </summary>
public interface IFrontmatterParser
{
    /// <summary>
    /// Extract the frontmatter from the content file.
    /// </summary>
    /// <param name="site"></param>
    /// <param name="fileContent"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    Frontmatter? ParseFrontmatter(Site site, in string filePath, in string fileContent);

    /// <summary>
    /// Parse the app config file.
    /// </summary>
    /// <param name="configFileContent"></param>
    /// <returns></returns>
    Site ParseSiteSettings(string configFileContent);
}