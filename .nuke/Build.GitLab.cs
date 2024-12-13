using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.CI.GitLab;
using Nuke.Common.IO;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.Git;
using Serilog;

namespace SuCoS.NUKE;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible integrating the GitLab CI/CD.
/// </summary>
internal sealed partial class Build
{
    /// <summary>
    /// The GitLab CI/CD variables are injected by Nuke.
    /// </summary>
    private static GitLab GitLab => GitLab.Instance;

    [Parameter("GitLab private token")]
    public readonly string GitlabPrivateToken;

    [Parameter("GitLab ProjectId")]
    public readonly long GitLabProjectId = GitLab?.ProjectId ?? 0;

    [Parameter("GitLab API URL")]
    private static readonly string GitLabApiBaseUrl =
        Environment.GetEnvironmentVariable("CI_API_V4_URL");

    private static string Date =>
        DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private string PackageName => GitLab?.ProjectName ?? RegistryImage;

    /// <summary>
    /// Uploads the package to the GitLab generic package registry.
    /// One for each runtime identifier.
    /// </summary>
    /// <see href="https://docs.gitlab.com/ee/user/packages/generic_packages/"/>
    public Target GitLabUploadPackage => td => td
        .DependsOn(Publish)
        .Requires(() => GitlabPrivateToken)
        .Executes(async () =>
        {
            // The package name constructed using packageName, runtimeIdentifier, and Version
            var criName = ContainerRuntimeIdentifier is not null ? ContainerRuntimeIdentifier.Value.identifier : RuntimeIdentifier;
            var package = $"{PackageName}-{criName}-{CurrentTag}";

            // The filename of the package, constructed using the package variable
            var filename = $"{package}.zip";

            // The URL for the package in the GitLab generic package registry
            var packageLink = GitLabApiUrl($"packages/generic/{PackageName}/{CurrentTag}/{filename}");

            // Create the zip package
            var fullPath = Path.GetFullPath(filename);
            try
            {
                PublishDirectory.ZipTo(
                    fullPath,
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
                await using var fileStream = File.OpenRead(fullPath);
                using var httpClient = HttpClientGitLabToken();
                var response = await httpClient.PutAsync(
                    packageLink,
                    new StreamContent(fileStream)).ConfigureAwait(false);

                _ = response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "{StatusCode}: {Message}", ex.StatusCode,
                    ex.Message);
                throw;
            }

            await GitLabCreateReleaseLink(package, packageLink);
        });

    /// <summary>
    /// Creates a release in the GitLab repository.
    /// </summary>
    /// <see href="https://docs.gitlab.com/ee/api/releases/#create-a-release"/>
    public Target GitLabCreateRelease => td => td
        .DependsOn(GitLabCreateTag)
        .OnlyWhenStatic(() => HasNewCommits)
        .Requires(() => GitlabPrivateToken)
        .Executes(async () =>
        {
            try
            {
                using var httpClient = HttpClientGitLabToken();
                var message = $"Created in {Date}";
                var release = $"{TagName} / {Date}";
                var response = await httpClient.PostAsJsonAsync(
                    GitLabApiUrl("releases"),
                    new
                    {
                        tag_name = TagName,
                        name = release,
                        description = message
                    }).ConfigureAwait(false);

                _ = response.EnsureSuccessStatusCode();
                Log.Information(
                    "Release {release} created with the description '{message}'",
                    release, message);
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "{StatusCode}: {Message}", ex.StatusCode,
                    ex.Message);
                throw;
            }
        });

    /// <summary>
    /// Creates a tag in the GitLab repository.
    /// </summary>
    /// <see href="https://docs.gitlab.com/ee/api/tags.html#create-a-new-tag"/>
    private Target GitLabCreateTag => td => td
        .DependsOn(CheckNewCommits, GitLabCreateCommit)
        .OnlyWhenStatic(() => HasNewCommits)
        .Requires(() => GitlabPrivateToken)
        .Executes(async () =>
        {
            try
            {
                using var httpClient = HttpClientGitLabToken();
                var message = $"Automatic tag creation: '{TagName}' in {Date}";
                var response = await httpClient.PostAsJsonAsync(
                    GitLabApiUrl("repository/tags"),
                    new
                    {
                        tag_name = TagName,
                        @ref = GitLab?.CommitRefName ??
                               GitTasks.GitCurrentCommit(),
                        message
                    }).ConfigureAwait(false);

                _ = response.EnsureSuccessStatusCode();
                Log.Information(
                    "Tag {tag} created with the message '{message}'",
                    TagName, message);
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "{StatusCode}: {Message}", ex.StatusCode,
                    ex.Message);
                throw;
            }
        });

    /// <summary>
    /// Push all images created to the Registry
    /// </summary>
    public Target GitLabPushContainer => td => td
        .DependsOn(CreateContainer)
        .OnlyWhenStatic(() => RuntimeIdentifier != "win-x64")
        .Executes(async () =>
        {
            // Log in to the Docker registry
            DockerTasks.DockerLogin(c => c
                .SetServer("registry.gitlab.com")
                .SetUsername("gitlab-ci-token")
                .SetPassword(GitLab.JobToken)
            );

            // Push the container images
            var tags = ContainerTags();
            foreach (var tag in tags)
            {
                DockerTasks.DockerPush(s => s
                    .SetName($"{RegistryImage}:{tag}")
                );

                // Create a link to the GitLab release
                var tagLink =
                    GitLabApiUrl($"?orderBy=NAME&sort=asc&search[]={tag}");
                await GitLabCreateReleaseLink($"docker-{tag}", tagLink);
            }
        });

    /// <summary>
    /// Upload the Debian package
    /// </summary>
    public Target GitLabPushDebianPackage => td => td
        .DependsOn(CreateDebianPackage)
        .Executes(async () =>
        {
            try
            {
                await using var fileStream = File.OpenRead(DebianPackage);
                using var httpClient = HttpClientGitLabToken();
                // https://docs.gitlab.com/ee/user/packages/debian_repository/#upload-a-package-with-explicit-distribution-and-component
                var response = await httpClient.PutAsync(
                    GitLabApiUrl($"packages/debian/SuCoS.deb?distribution={DebianDistribution}&component={DebianComponent}"),
                    new StreamContent(fileStream)).ConfigureAwait(false);

                _ = response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "{StatusCode}: {Message}", ex.StatusCode,
                    ex.Message);
                throw;
            }
        });

    private Target GitLabCreateCommit => td => td
        .DependsOn(CheckNewCommits, UpdateProjectVersions, UpdateChangelog)
        .OnlyWhenStatic(() => HasNewCommits)
        .Executes(async () =>
        {
            const string ciSkip = "#ci-skip";
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };

            // Get the list of CHANGED files, ignoring the created, moved or deleted ones
            var actions = GitTasks.Git("diff --name-only --diff-filter=M")
                .Select(x => x.Text)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(filePath =>
                {
                    var action = new CommitAction
                    {
                        action = CommitAction.ActionType.update.ToString(),
                        file_path = filePath,
                        content = File.ReadAllText(Path.Combine(
                            Path.GetDirectoryName(Solution.Path)!, filePath))
                    };

                    return action;
                })
                .ToList();

            try
            {
                using var httpClient = HttpClientGitLabToken();
                var message =
                    $"chore: Automatic commit creation in {Date} {ciSkip}";
                var response = await httpClient.PostAsJsonAsync(
                    GitLabApiUrl("repository/commits"),
                    new
                    {
                        branch = Repository.Branch,
                        commit_message = message,
                        actions
                    }, options).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                Log.Information(
                    "Commit in branch {branch} created with the message '{message}'",
                    Repository.Branch, message);
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "{StatusCode}: {Message}", ex.StatusCode,
                    ex.Message);
                throw;
            }
        });

    /// <summary>
    /// Creates an HTTP client and set the authentication header.
    /// </summary>
    private HttpClient HttpClientGitLabToken()
    {
        var httpClient = new HttpClient();
        if (string.IsNullOrEmpty(GitlabPrivateToken))
        {
            httpClient.DefaultRequestHeaders.Add("JOB_TOKEN", GitLab.JobToken);
        }
        else
        {
            httpClient.DefaultRequestHeaders.Add("Private-Token",
                GitlabPrivateToken);
        }

        return httpClient;
    }

    /// <summary>
    /// Generate the GitLab API URL.
    /// </summary>
    /// <param name="url">The URL to append to the base URL.</param>
    /// <returns></returns>
    private string GitLabApiUrl(string url)
    {
        var apiUrl = $"{GitLabApiBaseUrl}/projects/{GitLabProjectId}/{url}";
        Log.Information("GitLab API call: {url}", apiUrl);
        return apiUrl;
    }

    private async Task GitLabCreateReleaseLink(string itemName, string itemLink)
    {
        try
        {
            using var httpClient = HttpClientGitLabToken();
            var response = await httpClient.PostAsJsonAsync(
                GitLabApiUrl($"releases/{TagName}/assets/links"),
                new
                {
                    name = itemName,
                    url = itemLink
                }).ConfigureAwait(false);

            _ = response.EnsureSuccessStatusCode();
            Log.Information("Link added in release {tag}: '{package}'",
                TagName, itemLink);
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "{StatusCode}: {Message}", ex.StatusCode, ex.Message);
            throw;
        }
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    // ReSharper disable InconsistentNaming
    internal class CommitAction
    {
        public enum ActionType
        {
            create,
            delete,
            move,
            update,
            chmod
        }

        public required string action;
        public required string file_path;
        public string previous_path;
        public string content;
        public string encoding;
        public string last_commit_id;
        public bool execute_filemode;
    }
    // ReSharper restore InconsistentNaming
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}
