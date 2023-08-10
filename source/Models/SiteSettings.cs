
using System.Collections.Generic;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, extracted from the app.yaml file.
/// </summary>
public class SiteSettings : IParams
{
    /// <summary>
    /// Site Title/Name.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Site description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Copyright information
    /// </summary>
    public string? Copyright { get; set; }

    /// <summary>
    /// The base URL that will be used to build public links.
    /// </summary>
    public string BaseURL { get; set; } = string.Empty;

    /// <summary>
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    public bool UglyURLs { get; set; }

    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = new();

    #endregion IParams
}