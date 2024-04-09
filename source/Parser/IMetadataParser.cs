using SuCoS.Models;

namespace SuCoS.Parser;

/// <summary>
/// Responsible for parsing the content metadata
/// </summary>
public interface IMetadataParser
{
    /// <summary>
    /// Extract the front matter from the content file.
    /// </summary>
    /// <param name="fileFullPath"></param>
    /// <param name="sourceContentPath"></param>
    /// <returns></returns>
    IFrontMatter? ParseFrontmatterAndMarkdownFromFile(in string fileFullPath, in string sourceContentPath);

    /// <summary>
    /// Extract the front matter from the content.
    /// </summary>
    /// <param name="fileFullPath"></param>
    /// <param name="fileContent"></param>
    /// <param name="fileRelativePath"></param>
    /// <returns></returns>
    IFrontMatter? ParseFrontmatterAndMarkdown(in string fileFullPath, in string fileRelativePath, in string fileContent);

    /// <summary>
    /// Parse the app config file.
    /// </summary>
    /// <param name="configFileContent"></param>
    /// <returns></returns>
    SiteSettings ParseSiteSettings(string configFileContent);
}