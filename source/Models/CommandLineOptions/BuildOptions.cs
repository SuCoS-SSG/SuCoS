using System.IO;

namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Command line options for the build command.
/// </summary>
internal class BuildOptions : GenerateOptions
{
    /// <summary>
    /// The path of the output files.
    /// </summary>
    public string Output { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="source"></param>
    /// <param name="output"></param>
    public BuildOptions(string source, string output)
    {
        Source = source;
        Output = string.IsNullOrEmpty(output) ? Path.Combine(source, "public") : output;
    }
}
