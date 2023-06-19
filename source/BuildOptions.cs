namespace SuCoS;

/// <summary>
/// Command line options for the build command.
/// </summary>
public class BuildOptions : IGenerateOptions
{
    /// <inheritdoc/>
    public string Source { get; set; } = ".";

    /// <inheritdoc/>
    public string Output { get; set; } = "./public";

    /// <inheritdoc/>
    public bool Future { get; set; } = false;

    /// <inheritdoc/>
    public bool Verbose { get; set; } = false;
}
