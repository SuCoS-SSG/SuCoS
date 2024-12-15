namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, extracted from the app.yaml file.
/// </summary>
public interface ISiteSettings : IParams
{
    /// <summary>
    /// Site Title/Name.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Site description
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Copyright information
    /// </summary>
    string? Copyright { get; }

    /// <summary>
    /// The base URL that will be used to build public links.
    /// </summary>
    string BaseUrl { get; set; }

    /// <summary>
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    bool UglyUrLs { get; }

    /// <summary>
    /// The output format for each content kind
    /// </summary>
    Dictionary<Kind, List<string>> KindOutputFormats { get; }
}
