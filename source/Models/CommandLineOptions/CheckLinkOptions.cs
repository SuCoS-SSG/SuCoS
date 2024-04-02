using CommandLine;

namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Command line options for the checklinks command.
/// </summary>
[Verb("checklinks", HelpText = "Checks links of a given site")]
public class CheckLinkOptions
{
    /// <summary>
    /// How verbose it must be.
    /// </summary>
    [Option('v', "verbose", Required = false, HelpText = "How verbose it must be")]
    public bool Verbose { get; init; }

    /// <summary>
    /// The path of the source files.
    /// </summary>
    [Value(0, Default = "./")]
    public required string Source { get; init; }

    /// <summary>
    /// File names to be checked.
    /// </summary>
    [Option('f', "filters", Required = false, HelpText = "File name filters", Default = "*.html")]
    public required string Filters { get; init; }

    /// <summary>
    /// List of links to ignore checking.
    /// </summary>
    [Option('i', "ignore", Required = false, HelpText = "List of links to ignore checking")]
    public IEnumerable<string> Ignore { get; init; } = [];

    /// <summary>
    /// Site URL, so it can be checked as local path files.
    /// </summary>
    [Option('u', "url", Required = false, HelpText = "Site URL, so it can be checked as local path files.")]
    public string? InternalURL { get; init; }
}
