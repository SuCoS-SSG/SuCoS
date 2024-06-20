namespace SuCoS.Models;

/// <summary>
/// Page or Resources (files) that will be considered as output.
/// </summary>
public interface IOutput
{
    /// <summary>
    /// The URL for the content.
    /// </summary>
    public string? Permalink { get; set; }
}
