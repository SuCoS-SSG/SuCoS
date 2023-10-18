using Nuke.Common;
using Nuke.Common.CI.GitLab;
using Nuke.Common.IO;
using Nuke.Common.Tools.Git;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;

namespace SuCoS;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible integrating the GitLab CI/CD.
/// </summary>
sealed partial class Build : NukeBuild
{
    /// <summary>
    /// The GitLab CI/CD variables are injected by Nuke.
    /// </summary>
    static GitLab GitLab => GitLab.Instance;

    [Parameter("GitLab private token")]
    readonly string gitlabPrivateToken;

    [Parameter("If the pipeline was triggered by a schedule (or manually)")]
    readonly bool isScheduled;

    [Parameter("package-name (default: SuCoS)")]
    readonly string packageName = GitLab?.ProjectName ?? "SuCoS";

    // The base URL for the GitLab API
    static string CI_API_V4_URL => Environment.GetEnvironmentVariable("CI_API_V4_URL");

    static string Date => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Uploads the package to the GitLab generic package registry.
    /// One for each runtime identifier.
    /// </summary>
    /// <see href="https://docs.gitlab.com/ee/user/packages/generic_packages/"/>
    public Target CreatePackage => _ => _
        .DependsOn(Publish)
        .DependsOn(CheckNewCommits)
        .Requires(() => gitlabPrivateToken)
        .Executes(async () =>
        {
            // The package name constructed using packageName, runtimeIdentifier, and Version
            var package = $"{packageName}-{runtimeIdentifier}-{CurrentTag}";

            // The filename of the package, constructed using the package variable
            var filename = $"{package}.zip";

            // The URL for the package in the GitLab generic package registry
            var packageLink = GitLabAPIUrl($"packages/generic/{packageName}/{CurrentTag}/{filename}");

            // Create the zip package
            var fullpath = Path.GetFullPath(filename);
            try
            {
                PublishDirectory.ZipTo(
                    fullpath,
                    filter: x => !x.HasExtension("pdb", "xml"),
                    compressionLevel: CompressionLevel.Optimal,
                    fileMode: FileMode.Create // overwrite if exists
                    );
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error zip");
                throw;
            }

            try
            {
                using var fileStream = File.OpenRead(fullpath);
                using var httpClient = HttpClientGitLabToken();
                var response = await httpClient.PutAsync(
                    packageLink,
                    new StreamContent(fileStream));

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating package");
                throw;
            }

            GitLabCreateReleaseLink(package, packageLink);
        });

    /// <summary>
    /// Creates a release in the GitLab repository.
    /// </summary>
    /// <see href="https://docs.gitlab.com/ee/api/releases/#create-a-release"/>
    public Target GitLabCreateRelease => _ => _
        .DependsOn(GitLabCreateTag)
        .OnlyWhenStatic(() => HasNewCommits)
        .Requires(() => gitlabPrivateToken)
        .Executes(async () =>
        {
            try
            {
                using var httpClient = HttpClientGitLabToken();
                var response = await httpClient.PostAsJsonAsync(
                    GitLabAPIUrl("releases"),
                    new
                    {
                        tag_name = TagName,
                        name = $"{TagName} {Date}",
                        description = $"Created {Date}"
                    });

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating release");
                throw;
            }
        });

    /// <summary>
    /// Creates a tag in the GitLab repository.
    /// </summary>
    /// <see href="https://docs.gitlab.com/ee/api/tags.html#create-a-new-tag"/>
    Target GitLabCreateTag => _ => _
        .DependsOn(CheckNewCommits)
        .After(Compile)
        .OnlyWhenStatic(() => HasNewCommits)
        .Requires(() => gitlabPrivateToken)
        .Executes(async () =>
        {
            try
            {
                using var httpClient = HttpClientGitLabToken();
                var response = await httpClient.PostAsJsonAsync(
                    GitLabAPIUrl("repository/tags"),
                    new
                    {
                        tag_name = TagName,
                        @ref = GitLab?.CommitRefName ?? GitTasks.GitCurrentCommit(),
                        message = $"Automatic tag creation: {isScheduled} at {Date}"
                    });

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating tag");
                throw;
            }
        });

    /// <summary>
    /// Creates a HTTP client and set the authentication header.
    /// </summary>
    /// <param name="useJobToken">If the job token should be used instead of the private token.</param>
    HttpClient HttpClientGitLabToken(bool useJobToken = false)
    {
        var httpClient = new HttpClient();
        if (useJobToken)
        {
            httpClient.DefaultRequestHeaders.Add("JOB_TOKEN", GitLab.JobToken);
        }
        else
        {
            httpClient.DefaultRequestHeaders.Add("Private-Token", gitlabPrivateToken);
        }
        return httpClient;
    }

    /// <summary>
    /// Generate the GitLab API URL.
    /// </summary>
    /// <param name="url">The URL to append to the base URL.</param>
    /// <returns></returns>
    static string GitLabAPIUrl(string url)
    {
        var apiUrl = $"{CI_API_V4_URL}/projects/{GitLab.ProjectId}/{url}";
        Log.Information("GitLab API call: {url}", apiUrl);
        return apiUrl;
    }

    async void GitLabCreateReleaseLink(string itemName, string itemLink)
    {
        try
        {
            using var httpClient = HttpClientGitLabToken();
            var response = await httpClient.PostAsJsonAsync(
                GitLabAPIUrl($"releases/{TagName}/assets/links"),
                new
                {
                    name = itemName,
                    url = itemLink
                });

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating release");
            throw;
        }
    }
}
