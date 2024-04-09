using YamlDotNet.Serialization;

namespace SuCoS.Models;

/// <summary>
/// Representation of the theme.
/// </summary>
[YamlSerializable]
public class Theme
{
    /// <summary>
    /// Theme name
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Theme name
    /// </summary>
    [YamlIgnore]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The path of the static content (that will be copied as is)
    /// </summary>
    [YamlIgnore]
    public string StaticFolder => System.IO.Path.Combine(Path, "static");

    /// <summary>
    /// folder that contains default layout files.
    /// </summary>
    [YamlIgnore]
    public string DefaultLayoutFolder => System.IO.Path.Combine(Path, "_default");

    /// <summary>
    /// All default folders
    /// </summary>
    [YamlIgnore]
    public IEnumerable<string> Folders => [
        StaticFolder
        ];

    /// <summary>
    /// Create a Theme from a given metadata content.
    /// </summary>
    /// <param name="site"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Theme Create(Site site, string data)
    {
        ArgumentNullException.ThrowIfNull(site);

        var theme = site.Parser.Parse<Theme>(data);
        theme.Path = site.SourceThemePath;
        return theme;
    }

    /// <summary>
    /// Create a Theme from a given metadata file path.
    /// </summary>
    /// <param name="site"></param>
    /// <returns></returns>
    public static Theme? CreateFromSite(Site site)
    {
        ArgumentNullException.ThrowIfNull(site);

        var path = System.IO.Path.Combine(site.SourceThemePath, "sucos.yaml");

        if (File.Exists(path))
        {
            var data = File.ReadAllText(path);
            return Create(site, data);
        }
        return null;
    }
}
