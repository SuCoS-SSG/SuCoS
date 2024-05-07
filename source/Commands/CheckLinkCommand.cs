using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Serilog;
using SuCoS.Models.CommandLineOptions;

namespace SuCoS.Commands;

/// <summary>
/// Check links of a given site.
/// </summary>
public sealed partial class CheckLinkCommand(CheckLinkOptions settings, ILogger logger)
{
    [GeneratedRegex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9@:%_\+.~#?&\/=]*)")]
    private static partial Regex UrlGeneratedRegex();
    private static readonly Regex UrlRegex = UrlGeneratedRegex();
    private const int RetriesCount = 3;
    private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(1);
    private HttpClient _httpClient = null!;
    private readonly ConcurrentBag<string> _checkedLinks = [];
    private readonly ConcurrentDictionary<string, List<string>> _linkToFilesMap = [];
    private readonly ConcurrentBag<string> _failedLinks = [];

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

        _httpClient = GetHttpClient();

        var files = GetFiles(directoryPath, settings.Filters);
        var linksAreValid = await CheckLinks(directoryPath, files, _httpClient).ConfigureAwait(false);

        if (!linksAreValid)
        {
            logger.Error("There are failed checks.");

            foreach (var (link, linkFiles) in _linkToFilesMap)
            {
                if (_failedLinks.Contains(link))
                {
                    linkFiles.Sort();
                    logger.Error("Link {link} failed and are in these files:\n{files}", link, string.Join("\n", linkFiles));
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
        // var filesCount = files.Length;
        var result = true;

        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        await Parallel.ForEachAsync(files, options, async (filePath, token) =>
        {
            var fileNameSanitized = filePath[directoryPath.Length..].Trim('/', '\\');
            var fileText = await File.ReadAllTextAsync(filePath, token);
            var matches = UrlRegex.Matches(fileText);
            if (matches.Count == 0)
            {
                LogInformation("{fileName}: no links found", fileNameSanitized);
                return;
            }

            LogInformation("{fileName}: {matches} link found", fileNameSanitized, matches.Count.ToString(CultureInfo.InvariantCulture));
            foreach (Match match in matches)
            {
                var link = match.Value.Trim('.');

                if (!_linkToFilesMap.TryGetValue(link, out var value))
                {
                    value = [];
                    _linkToFilesMap[link] = value;
                }

                if (!value.Contains(fileNameSanitized))
                {
                    value.Add(fileNameSanitized);
                }
                if (_checkedLinks.Contains(link))
                {
                    continue;
                }
                _checkedLinks.Add(link);

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
                for (var j = 0; j < RetriesCount && !linkIsValid; j++)
                {
                    linkIsValid |= await CheckLink(fileNameSanitized, link, httpClient).ConfigureAwait(false);
                    if (!linkIsValid && j < RetriesCount - 1)
                    {
                        LogInformation("{fileName}: {link} retrying...", fileNameSanitized, link);
                        Thread.Sleep(_retryInterval);
                    }
                }

                if (linkIsValid)
                {
                    LogInformation("{fileName}: {link} OK", fileNameSanitized, link);
                }
                else
                {
                    LogError("{fileName}: {link} FAIL", fileNameSanitized, link);
                    _failedLinks.Add(link);
                }

                result &= linkIsValid;
            }
        }).ConfigureAwait(false);

        return result;
    }

    private bool TryLocalFile(CheckLinkOptions options, string directoryPath, string fileNameSanitized, string link)
    {
        if (string.IsNullOrEmpty(options.InternalUrl) || !link.StartsWith(options.InternalUrl))
        {
            return false;
        }

        // Strip the InternalURL from the link
        link = link[options.InternalUrl.Length..];

        // Handle the link as a local file
        var localFilePath = Path.Combine(directoryPath, link);
        if (File.Exists(localFilePath))
        {
            LogInformation("{fileName}: {link} is a local file", fileNameSanitized, link);
        }
        else
        {
            LogError("{fileName}: {link} is a local file but does not exist", fileNameSanitized, link);
            _failedLinks.Add(link);
        }
        _checkedLinks.Add(link);

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
            _failedLinks.Add(link);
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
