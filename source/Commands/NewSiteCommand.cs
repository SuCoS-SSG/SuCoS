using Serilog;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS;

/// <summary>
/// Check links of a given site.
/// </summary>
public sealed partial class NewSiteCommand(NewSiteOptions options, ILogger logger)
{
    /// <summary>
    /// Run the app
    /// </summary>
    /// <returns></returns>
    public int Run()
    {
        var siteSettings = new SiteSettings()
        {
            Title = options.Title,
            Description = options.Description,
            BaseURL = options.BaseURL
        };

        // TODO: Refactor Site class to not need YAML parser nor FrontMatterParser
        var site = new Site(new ServeOptions() { SourceOption = options.Output }, siteSettings, null!, logger, null);

        var outputPath = Path.GetFullPath(options.Output);
        var siteSettingsPath = Path.Combine(outputPath, "sucos.yaml");

        if (File.Exists(siteSettingsPath) && !options.Force)
        {
            logger.Error("{directoryPath} already exists", outputPath);
            return 1;
        }

        logger.Information("Creating a new site: {title} at {outputPath}", siteSettings.Title, outputPath);

        CreateFolders(site.SourceFodlers);

        try
        {
            site.Parser.Export(siteSettings, siteSettingsPath);
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
}