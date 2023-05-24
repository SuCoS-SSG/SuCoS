namespace SuCoS;

/// <summary>
/// Command line options for the build command.
/// </summary>
public class BuildOptions
{
    /// <summary>
    /// The path of the source files.
    /// </summary>
    public string Source { get; set; } = ".";

    /// <summary>
    /// The path of the output files.
    /// </summary>
    public string Output { get; set; } = "./public";

    /// <summary>
    /// If true, the program will print more information.
    /// </summary>
    public bool Verbose { get; set; } = false;
}
