namespace SuCoS.Parsers;

/// <summary>
/// Responsible for parsing the content front matter
/// </summary>
public interface IFrontMatterParser
{
    /// <summary>
    /// Extract the front matter from the content.
    /// </summary>
    /// <param name="fileContent"></param>
    /// <returns></returns>
    (string frontMatter, string rawContent) SplitFrontMatterAndContent(in string fileContent);

    /// <summary>
    /// Parse a string content to the T class.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    T Parse<T>(string content);

    /// <summary>
    /// Deserialize an object into a file.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="fileFullPath"></param>
    void SerializeAndSave<T>(T data, string fileFullPath);
}
