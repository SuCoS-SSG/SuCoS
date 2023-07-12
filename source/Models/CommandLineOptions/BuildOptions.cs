namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Command line options for the build command.
/// </summary>
public class BuildOptions : GenerateOptions
{
    /// <summary>
    /// The path of the output files.
    /// </summary>
    public string Output { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="output"></param>
    public BuildOptions(string output)
    {
        Output = output;
    }
}
