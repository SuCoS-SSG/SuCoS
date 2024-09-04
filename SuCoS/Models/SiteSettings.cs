using YamlDotNet.Serialization;

namespace SuCoS.Models;

/// <summary>
/// The main configuration of the program, extracted from the app.yaml file.
/// </summary>
[YamlSerializable]
public class SiteSettings : ISiteSettings
{
    #region ISiteSettings

    /// <inheritdoc/>
    public string Title { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Description { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Copyright { get; set; }

    /// <inheritdoc/>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Types of outputs
    /// </summary>
    public Dictionary<string, List<string>> Outputs { get; set; } = [];

    #endregion ISiteSettings

    /// <summary>
    /// The global site theme.
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// The theme folder where all themes are placed (if any).
    /// </summary>
    public string ThemeDir { get; set; } = "themes";

    /// <summary>
    /// The appearance of a URL is either ugly or pretty.
    /// </summary>
    public bool UglyUrLs { get; set; }

    #region IParams

    /// <inheritdoc/>
    public Dictionary<string, object> Params { get; set; } = [];

    #endregion IParams
}
