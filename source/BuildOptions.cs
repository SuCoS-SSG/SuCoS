namespace SuCoS;

/// <summary>
/// Command line options for the build command.
/// </summary>
public class BuildOptions : IGenerateOptions
{
    /// <inheritdoc/>
    public string Source { get; set; } = ".";

    /// <inheritdoc/>
    public string? Output { get; init; }

    /// <inheritdoc/>
    public bool Future { get; set; }
}
