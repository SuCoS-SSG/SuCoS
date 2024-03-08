using CommandLine;

namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Basic Command line options for the serve and build command.
/// </summary>
public class GenerateOptions : IGenerateOptions
{
    /// <inheritdoc/>
    [Option('v', "verbose", Required = false, HelpText = "How verbose it must be")]
    public bool Verbose { get; init; }

    /// <inheritdoc/>
    [Option('s', "source", Required = false, HelpText = "Source directory path")]
    public required string Source { get; init; } = ".";

    /// <inheritdoc/>
    [Option('d', "draft", Required = false, HelpText = "Include draft content")]
    public bool Draft { get; init; }

    /// <inheritdoc/>
    [Option('f', "future", Required = false, HelpText = "Include content with dates in the future")]
    public bool Future { get; init; }

    /// <inheritdoc/>
    [Option('e', "expired", Required = false, HelpText = "Include content with ExpiredDate dates from the past")]
    public bool Expired { get; init; }
}
