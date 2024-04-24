using CommandLine;

namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Command line options to generate a simple site from scratch.
/// </summary>
[Verb("new-site", false, HelpText = "Generate a simple site from scratch")]
public class NewSiteOptions
{
    /// <summary>
    /// The path of the output files.
    /// </summary>
    [Option('o', "output", Required = false, HelpText = "Output directory path")]
    public required string Output { get; init; }

    /// <summary>
    /// Force site creation.
    /// </summary>
    [Option('f', "force", Required = false, HelpText = "Force site creation")]
    public bool Force { get; init; }

    /// <summary>
    /// Site title.
    /// </summary>
    [Option("title", Required = false, HelpText = "Site title")]
    public string Title { get; init; } = "My Site";

    /// <summary>
    /// Site description.
    /// </summary>
    [Option("description", Required = false, HelpText = "Site description")]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Site base url.
    /// </summary>
    [Option("url", Required = false, HelpText = "Site base url")]
    public string BaseURL { get; init; } = "https://example.org/";
}
