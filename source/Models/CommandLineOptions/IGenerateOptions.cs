namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Command line options for the build and serve command.
/// </summary>
public interface IGenerateOptions
{
    /// <summary>
    /// How verbose it must be.
    /// </summary>
    bool Verbose { get; }

    /// <summary>
    /// The path of the source files.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// The path of the source files
    /// </summary>
    string SourceArgument { init; }

    /// <summary>
    /// The path of the source files, as --source commandline option
    /// </summary>
    string SourceOption { init; }

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