using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Serilog;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS;

/// <summary>
/// Check links of a given site.
/// </summary>
public sealed partial class CheckLinkCommand(CheckLinkOptions settings, ILogger logger)
{
    [GeneratedRegex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9@:%_\+.~#?&\/=]*)")]
    private static partial Regex URLRegex();
    private static readonly Regex urlRegex = URLRegex();
    private const int retriesCount = 3;
    private readonly TimeSpan retryInterval = TimeSpan.FromSeconds(1);
    private HttpClient httpClient = null!;
    private readonly ConcurrentBag<string> checkedLinks = [];
    private readonly ConcurrentDictionary<string, List<string>> linkToFilesMap = [];
    private readonly ConcurrentBag<string> failedLinks = [];

    /// <summary>
    /// Run the app
    /// </summary>
    /// <returns></returns>
    public async Task<int> Run()
    {
        var directoryPath = Path.GetFullPath(settings.Source);

        if (!Directory.Exists(directoryPath))
        {
            logger.Fatal("Directory '{directoryPath}' doesn't exist.", directoryPath);
            return 1;
        }

        httpClient = GetHttpClient();

        var files = GetFiles(directoryPath, settings.Filters);
        var linksAreValid = await CheckLinks(directoryPath, files, httpClient).ConfigureAwait(false);

        if (!linksAreValid)
        {
            logger.Error("There are failed checks.");

            foreach (var (link, linkfiles) in linkToFilesMap)
            {
                if (failedLinks.Contains(link))
                {
                    linkfiles.Sort();
                    logger.Error("Link {link} failed and are in these files:\n{files}", link, string.Join("\n", linkfiles));
                }
            }
            return 1;
        }
        logger.Information("Done");
        return 0;
    }

    private static HttpClient GetHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "C# App");
        return client;
    }

    private async Task<bool> CheckLinks(string directoryPath, string[] files, HttpClient httpClient)
    {
        var filesCount = files.Length;
        var result = true;

        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        await Parallel.ForEachAsync(files, options, async (filePath, token) =>
        {
            var fileNameSanitized = filePath[directoryPath.Length..].Trim('/', '\\');
            var fileText = File.ReadAllText(filePath);
            var matches = urlRegex.Matches(fileText);
            if (matches.Count == 0)
            {
                LogInformation("{fileName}: no links found", fileNameSanitized);
                return;
            }

            LogInformation("{fileName}: {matches} link found", fileNameSanitized, matches.Count.ToString(CultureInfo.InvariantCulture));
            foreach (Match match in matches)
            {
                var link = match.Value.Trim('.');

                if (!linkToFilesMap.TryGetValue(link, out var value))
                {
                    value = [];
                    linkToFilesMap[link] = value;
                }

                if (!value.Contains(fileNameSanitized))
                {
                    value.Add(fileNameSanitized);
                }
                if (checkedLinks.Contains(link))
                {
                    continue;
                }
                checkedLinks.Add(link);

                if (settings.Ignore.Contains(link))
                {
                    continue;
                }

                if (TryLocalFile(settings, directoryPath, fileNameSanitized, link))
                {
                    continue;
                }

                LogInformation("{fileName}: {link} found", fileNameSanitized, link);

                var linkIsValid = false;
                for (var j = 0; j < retriesCount && !linkIsValid; j++)
                {
                    linkIsValid |= await CheckLink(fileNameSanitized, link, httpClient).ConfigureAwait(false);
                    if (!linkIsValid && j < retriesCount - 1)
                    {
                        LogInformation("{fileName}: {link} retrying...", fileNameSanitized, link);
                        Thread.Sleep(retryInterval);
                    }
                }

                if (linkIsValid)
                {
                    LogInformation("{fileName}: {link} OK", fileNameSanitized, link);
                }
                else
                {
                    LogError("{fileName}: {link} FAIL", fileNameSanitized, link);
                    failedLinks.Add(link);
                }

                result &= linkIsValid;
            }
        }).ConfigureAwait(false);

        return result;
    }

    private bool TryLocalFile(CheckLinkOptions settings, string directoryPath, string fileNameSanitized, string link)
    {
        if (string.IsNullOrEmpty(settings.InternalURL) || !link.StartsWith(settings.InternalURL))
        {
            return false;
        }

        // Strip the InternalURL from the link
        link = link[settings.InternalURL.Length..];

        // Handle the link as a local file
        var localFilePath = Path.Combine(directoryPath, link);
        if (File.Exists(localFilePath))
        {
            LogInformation("{fileName}: {link} is a local file", fileNameSanitized, link);
        }
        else
        {
            LogError("{fileName}: {link} is a local file but does not exist", fileNameSanitized, link);
            failedLinks.Add(link);
        }
        checkedLinks.Add(link);

        return true;
    }

    private async Task<bool> CheckLink(string fileName, string link, HttpClient httpClient)
    {
        try
        {
            var response = await httpClient.GetAsync(link);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                LogError("{fileName}: {link} failed with: {response}", fileName, link, response.StatusCode);
            }

            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            LogError("{fileName}: {link} failed with: {exMessage}", fileName, link, ex.Message);
            failedLinks.Add(link);
            return false;
        }
    }

    private string[] GetFiles(string directoryPath, string filter)
    {
        logger.Information("Searching files in the directory '{directoryPath}' by '{filter}' filter...", directoryPath, filter);

        var files = Directory.GetFiles(directoryPath, filter, SearchOption.AllDirectories);

        logger.Information("{filesLength} files found", files.Length);
        return files;
    }

	private void LogInformation(string message, string fileName, string? link = null, string? arg = null)
    {
        if (settings.Verbose)
        {
            logger.Information(message, fileName, link, arg);
        }
    }

	private void LogError(string message, string fileName, string? link = null, string? arg = null)
    {
        if (settings.Verbose)
        {
            logger.Error(message, fileName, link, arg);
        }
    }
    
    private void LogError(string message, string fileName, string? link, HttpStatusCode arg)
    {
        if (settings.Verbose)
        {
            logger.Error(message, fileName, link, arg);
        }
    }
}