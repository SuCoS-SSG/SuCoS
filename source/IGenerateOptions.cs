namespace SuCoS;

/// <summary>
/// Command line options for the build and serve command.
/// </summary>
public interface IGenerateOptions
{
    /// <summary>
    /// The path of the source files.
    /// </summary>
    string Source { get; set; }

    /// <summary>
    /// The path of the output files.
    /// </summary>
    public string Output { get; set; }

    /// <summary>
    /// Consider 
    /// </summary>
    bool Future { get; set; }

    /// <summary>
    /// If true, the program will print more information.
    /// </summary>
    bool Verbose { get; set; }
}