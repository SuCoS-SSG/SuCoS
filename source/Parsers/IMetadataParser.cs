using SuCoS.Models;

namespace SuCoS.Parser;

/// <summary>
/// Responsible for parsing the content metadata
/// </summary>
public interface IMetadataParser
{
    /// <summary>
    /// Extract the front matter from the content.
    /// </summary>
    /// <param name="fileContent"></param>
    /// <returns></returns>
    (string, string) SplitFrontMatter(in string fileContent);

    /// <summary>
    /// Parse a string content to the T class.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    T Parse<T>(string content);

    /// <summary>
    /// Deserialized a object.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="path"></param>
    void Export<T>(T data, string path);
}