using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Serilog;
using System;
using System.Linq;

namespace SuCoS;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for create the Container (Docker) image.
/// </summary>
sealed partial class Build : NukeBuild
{
    [Parameter("GitLab Project CI_REGISTRY_IMAGE")]
    readonly string containerRegistryImage;

    string ContainerRegistryImage => containerRegistryImage ?? $"registry.gitlab.com/{GitLab?.ProjectPath}";

    [Parameter("GitLab Project Full Address")]
    readonly string containerDefaultRID = "linux-x64";

    public Target CreateContainer => _ => _
        .DependsOn(Publish)
        .DependsOn(CheckNewCommits)
        .OnlyWhenStatic(() => runtimeIdentifier != "win-x64")
        .Executes(() =>
        {
            var tagsOriginal = new[] { "latest", Version, VersionMajorMinor, VersionMajor };
            var tags = tagsOriginal.Select(tag => $"{runtimeIdentifier}-{tag}").ToList();
            if (containerDefaultRID == runtimeIdentifier)
            {
                tags.AddRange(tagsOriginal);
            }

            // Build the Container image
            DockerTasks.DockerBuild(_ => _
                .SetPath(PublishDirectory)
                .SetFile($"./Dockerfile")
                .SetTag(tags.Select(tag => $"{ContainerRegistryImage}:{tag}").ToArray())
                .SetBuildArg(new[] { $"BASE_IMAGE={BaseImage}", $"COPY_PATH={PublishDirectory}" })
                .SetProcessLogger((outputType, output) =>
                {
                    // A bug this log type value
                    if (outputType != OutputType.Std)
                        Log.Information(output);
                    else
                        Log.Error(output);
                })
                );

            // Log in to the Docker registry
            DockerTasks.DockerLogin(_ => _
                .SetServer("registry.gitlab.com")
                .SetUsername("gitlab-ci-token")
                .SetPassword(GitLab.JobToken)
                );

            // Push the container images
            foreach (var tag in tags)
            {
                DockerTasks.DockerPush(_ => _
                    .SetName($"{ContainerRegistryImage}:{tag}")
                    );

                // Create a link to the GitLab release
                var tagLink = GitLabAPIUrl($"?orderBy=NAME&sort=asc&search[]={tag}");
                GitLabCreateReleaseLink($"docker-{tag}", tagLink);
            }
        });

    string BaseImage => runtimeIdentifier switch
    {
        "linux-x64" => "ubuntu",
        "alpine-x64" => "alpine",
        _ => throw new ArgumentException($"There is no container image for: {runtimeIdentifier}"),
    };
}
