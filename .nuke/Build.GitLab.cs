using Nuke.Common;
using Nuke.Common.CI.GitLab;
using Nuke.Common.IO;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.Git;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;

namespace SuCoS.Nuke;

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
    public Target GitLabUploadPackage => td => td
        .DependsOn(Publish)
        .DependsOn(CheckNewCommits)
        .Requires(() => gitlabPrivateToken)
        .Executes(async () =>
        {
            // The package name constructed using packageName, runtimeIdentifier, and Version
            var rid = runtimeIdentifier != "linux-musl-x64" ? runtimeIdentifier : "alpine";
            var package = $"{packageName}-{rid}-{CurrentTag}";

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
                    new StreamContent(fileStream)).ConfigureAwait(false);

                _ = response.EnsureSuccessStatusCode();
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
    public Target GitLabCreateRelease => td => td
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
                    }).ConfigureAwait(false);

                _ = response.EnsureSuccessStatusCode();
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
    Target GitLabCreateTag => td => td
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
                    }).ConfigureAwait(false);

                _ = response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating tag");
                throw;
            }
        });

    /// <summary>
    /// Push all images created to the Registry
    /// </summary>
    public Target GitLabPushContainer => td => td
        .DependsOn(CreateContainer)
        .OnlyWhenStatic(() => runtimeIdentifier != "win-x64")
        .Executes(() =>
        {
            var tags = ContainerTags();

            // Log in to the Docker registry
            _ = DockerTasks.DockerLogin(_ => _
                .SetServer("registry.gitlab.com")
                .SetUsername("gitlab-ci-token")
                .SetPassword(GitLab.JobToken)
                );

            // Push the container images
            foreach (var tag in tags)
            {
                _ = DockerTasks.DockerPush(_ => _
                    .SetName($"{ContainerRegistryImage}:{tag}")
                    );

                // Create a link to the GitLab release
                var tagLink = GitLabAPIUrl($"?orderBy=NAME&sort=asc&search[]={tag}");
                GitLabCreateReleaseLink($"docker-{tag}", tagLink);
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
                }).ConfigureAwait(false);

            _ = response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating release");
            throw;
        }
    }
}