namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Command line options for the build and serve command.
/// </summary>
public interface IGenerateOptions
{
    /// <summary>
    /// The path of the source files.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Include draft content
    /// </summary>
    bool Draft { get; }

    /// <summary>
    /// Include future content
    /// </summary>
    bool Future { get; }

    /// <summary>
    /// Include expired content
    /// </summary>
    bool Expired { get; }
}