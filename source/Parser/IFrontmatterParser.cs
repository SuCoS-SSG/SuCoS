using SuCoS.Models;

namespace SuCoS.Parser;

/// <summary>
/// Responsible for parsing the content front matter
/// </summary>
public interface IFrontMatterParser
{
    /// <summary>
    /// Extract the front matter from the content file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="sourceContentPath"></param>
    /// <returns></returns>
    IFrontMatter? ParseFrontmatterAndMarkdownFromFile(in string filePath, in string sourceContentPath);

    /// <summary>
    /// Extract the front matter from the content.
    /// </summary>
    /// <param name="fileContent"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    IFrontMatter? ParseFrontmatterAndMarkdown(in string filePath, in string fileContent);

    /// <summary>
    /// Parse the app config file.
    /// </summary>
    /// <param name="configFileContent"></param>
    /// <returns></returns>
    SiteSettings ParseSiteSettings(string configFileContent);
}