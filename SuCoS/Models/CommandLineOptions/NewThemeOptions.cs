using CommandLine;

namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Command line options to generate a simple site from scratch.
/// </summary>
[Verb("new-theme", HelpText = "Generate a simple theme from scratch")]
public class NewThemeOptions
{
    /// <summary>
    /// The path of the output files.
    /// </summary>
    [Option('o', "output", Required = false, HelpText = "Output directory path")]
    public required string Output { get; init; }

    /// <summary>
    /// Force theme creation.
    /// </summary>
    [Option('f', "force", Required = false, HelpText = "Force theme creation")]
    public bool Force { get; init; }

    /// <summary>
    /// Theme title.
    /// </summary>
    [Option("title", Required = false, HelpText = "Theme title")]
    public required string Title { get; init; } = "My Theme";
}
