namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, extracted from the app.yaml file.
/// </summary>
public interface ISiteSettings : IParams
{
    /// <summary>
    /// Site Title/Name.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Site description
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Copyright information
    /// </summary>
    public string? Copyright { get; }

    /// <summary>
    /// The base URL that will be used to build public links.
    /// </summary>
    public string BaseURL { get; }

    /// <summary>
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    public bool UglyURLs { get; }
}
