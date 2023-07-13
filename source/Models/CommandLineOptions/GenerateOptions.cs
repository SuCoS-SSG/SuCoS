namespace SuCoS.Models.CommandLineOptions;

/// <summary>
/// Basic Command line options for the serve and build command.
/// </summary>
internal class GenerateOptions : IGenerateOptions
{
    /// <inheritdoc/>
    public string Source { get; init; } = ".";

    /// <inheritdoc/>
    public bool Future { get; init; }

    /// <inheritdoc/>
    public bool Expired { get; init; }
}
