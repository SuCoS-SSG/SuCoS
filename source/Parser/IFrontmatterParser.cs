namespace SuCoS;

/// <summary>
/// Responsible for parsing the content frontmatter
/// </summary>
public interface IFrontmatterParser
{
    /// <summary>
    /// Extract the frontmatter from the content file.
    /// </summary>
    /// <param name="fileContent"></param>
    /// <returns></returns>
    Frontmatter ParseFrontmatter(ref string fileContent);

    /// <summary>
    /// Parse the app config file.
    /// </summary>
    /// <param name="configFileContent"></param>
    /// <returns></returns>
    AppConfig ParseAppConfig(string configFileContent);
}