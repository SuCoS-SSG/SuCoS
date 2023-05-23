using System.IO;

namespace SuCoS;

/// <summary>
/// The main configuration of the program, primarily extracted from the app.yaml file.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Site Title.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// The base URL that will be used to build internal links.
    /// </summary>
    public string BaseUrl { get; set; } = "./";

    /// <summary>
    /// The base path of the source site files.
    /// </summary>
    public string SourcePath { get; set; } = "./";

    /// <summary>
    /// The path where the generated site files will be saved.
    /// </summary>
    public string OutputPath { get; set; } = "./";

    /// <summary>
    /// The path of the content, based on the source path.
    /// </summary>
    public string SourceContentPath => Path.Combine(SourcePath, "content");
}
