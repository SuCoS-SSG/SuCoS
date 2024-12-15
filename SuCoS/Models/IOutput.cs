namespace SuCoS.Models;

/// <summary>
/// Page or Resources (files) that will be considered as output.
/// </summary>
public interface IOutput
{
    /// <summary>
    /// The URL for the content.
    /// </summary>
    string Permalink => new Uri(new Uri(Site.BaseUrl), RelPermalink).ToString();

    /// <summary>
    /// The relative permalink's "path"
    /// </summary>
    string PermalinkDir => new Uri(new Uri(Permalink), ".").ToString();

    /// <summary>
    /// The relative permalink's filename
    /// </summary>
    string PermalinkFilename => Path.GetFileName(Permalink);

    /// <summary>
    /// The URL for the content.
    /// </summary>
    string RelPermalink { get; set; }

    /// <summary>
    /// The relative permalink's "path"
    /// </summary>
    string RelPermalinkDir => Path.GetDirectoryName(RelPermalink) ?? "/";

    /// <summary>
    /// The relative permalink's filename
    /// </summary>
    string RelPermalinkFilename => Path.GetFileName(RelPermalink);

    /// <summary>
    /// Point to the site configuration.
    /// </summary>
    ISite Site { get; }
}
