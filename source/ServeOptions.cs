namespace SuCoS;

/// <summary>
/// Command line options for the serve command.
/// </summary>
public class ServeOptions : IGenerateOptions
{
    /// <inheritdoc/>
    public string Source { get; set; } = ".";

    /// <inheritdoc/>
    public string Output { get; set; } = "./public";

    /// <inheritdoc/>
    public bool Future { get; set; } = false;
}
