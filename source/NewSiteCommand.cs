using Serilog;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using YamlDotNet.Serialization;

namespace SuCoS;

/// <summary>
/// Check links of a given site.
/// </summary>
public sealed partial class NewSiteCommand(NewSiteOptions settings, ILogger logger)
{
    /// <summary>
    /// Run the app
    /// </summary>
    /// <returns></returns>
    public int Run()
    {
        var siteSettings = new SiteSettings()
        {
            Title = settings.Title,
            Description = settings.Description,
            BaseURL = settings.BaseURL,
        };

        // TODO: Refactor Site class to not need YAML parser nor FrontMatterParser
        var site = new Site(new ServeOptions() { SourceOption = settings.Output }, siteSettings, null!, logger, null);

        var outputPath = Path.GetFullPath(settings.Output);
        var siteSettingsPath = Path.Combine(outputPath, "sucos.yaml");

        if (File.Exists(siteSettingsPath) && !settings.Force)
        {
            logger.Error("{directoryPath} already exists", outputPath);
            return 1;
        }

        logger.Information("Creating a new site: {title} at {outputPath}", siteSettings.Title, outputPath);

        CreateFolders(site.SourceFodlers);

        try
        {
            ExportSiteSettings(siteSettings, siteSettingsPath);
        }
        catch (Exception ex)
        {
            logger.Error("Failed to export site settings: {ex}", ex);
            return 1;
        }
        logger.Information("Done");

        return 0;
    }

    /// <summary>
    /// Create the standard folders
    /// </summary>
    /// <param name="folders"></param>
    private void CreateFolders(IEnumerable<string> folders)
    {
        foreach (var folder in folders)
        {
            logger.Information("Creating {folder}", folder);
            Directory.CreateDirectory(folder);
        }
    }

    // TODO: move all YAML parsing to this own class
    #region YAML
    /// <summary>
    /// YamlDotNet parser to loosely parse the YAML file. Used to include all non-matching fields
    /// into Params.
    /// </summary>
    ISerializer yamlDeserializer = new SerializerBuilder()
        .IgnoreFields()
        .ConfigureDefaultValuesHandling(
            DefaultValuesHandling.OmitEmptyCollections
            | DefaultValuesHandling.OmitDefaults
            | DefaultValuesHandling.OmitNull)
        .Build();

    void ExportSiteSettings(SiteSettings siteSettings, string siteSettingsPath)
    {
        var siteSettingsConverted = yamlDeserializer.Serialize(siteSettings);
        File.WriteAllText(siteSettingsPath, siteSettingsConverted);
    }
    #endregion YAML
}