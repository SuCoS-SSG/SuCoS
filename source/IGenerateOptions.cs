namespace SuCoS;

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
    /// The path of the output files.
    /// </summary>
    public string? Output { get; }

    /// <summary>
    /// Consider 
    /// </summary>
    bool Future { get; }
}