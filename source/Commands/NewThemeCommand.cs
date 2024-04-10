using Serilog;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parser;

namespace SuCoS;

/// <summary>
/// Check links of a given site.
/// </summary>
public sealed partial class NewThemeCommand(NewThemeOptions options, ILogger logger)
{
    /// <summary>
    /// Run the app
    /// </summary>
    /// <returns></returns>
    public int Run()
    {
        var theme = new Theme()
        {
            Title = options.Title,
            Path = options.Output
        };
        var outputPath = Path.GetFullPath(options.Output);
        var themePath = Path.Combine(outputPath, "sucos.yaml");

        if (File.Exists(themePath) && !options.Force)
        {
            logger.Error("{directoryPath} already exists", outputPath);
            return 1;
        }

        logger.Information("Creating a new site: {title} at {outputPath}", theme.Title, outputPath);

        CreateFolders(theme.Folders);

        foreach (var themeFolder in theme.Folders)

            try
            {
                new YAMLParser().Export(theme, themePath);
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