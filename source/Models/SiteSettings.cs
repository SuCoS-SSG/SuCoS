
using System.Collections.Generic;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, extracted from the app.yaml file.
/// </summary>
public class SiteSettings : IParams
{
    /// <summary>
    /// Site Title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The base URL that will be used to build internal links.
    /// </summary>
    public string BaseUrl { get; set; } = "./";

    /// <summary>
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    public bool UglyURLs { get; set; } = false;

    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = new();

    #endregion IParams
}