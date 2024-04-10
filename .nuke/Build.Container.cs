using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuCoS.Nuke;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for create the Container (Docker) image.
/// </summary>
sealed partial class Build : NukeBuild
{
    [Parameter("GitLab Project CI_REGISTRY_IMAGE")]
    readonly string containerRegistryImage;

    string ContainerRegistryImage => containerRegistryImage ?? "sucos";

    [Parameter("GitLab Project Full Address")]
    readonly string containerDefaultRID = "linux-x64";

    /// <summary>
    /// Generate the Container image
    /// </summary>
    public Target CreateContainer => td => td
        .After(Publish)
        .DependsOn(CheckNewCommits)
        .OnlyWhenStatic(() => runtimeIdentifier != "win-x64")
        .Executes(() =>
        {
            var tags = ContainerTags();

            // Build the Container image
            _ = DockerTasks.DockerImageBuild(dbs => dbs
                .SetPath(PublishDirectory)
                .SetFile($"./Dockerfile")
                .SetTag(tags.Select(tag => $"{ContainerRegistryImage}:{tag}").ToArray())
                .SetBuildArg([$"BASE_IMAGE={BaseImage}", $"COPY_PATH={PublishDirectory}"])
                .SetProcessLogger((outputType, output) =>
                {
                    if (outputType == OutputType.Std)
                        Log.Information(output);
                    else
                        Log.Debug(output);
                })
                );
        });

    string BaseImage => runtimeIdentifier switch
    {
        "linux-x64" => "mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled",
        "linux-musl-x64" => "mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine",
        _ => throw new ArgumentException($"There is no container image for: {runtimeIdentifier}"),
    };

    /// <summary>
    /// Return the proper image tag for a given OS. For the second tupple value, if the image mush be marked as "latest"
    /// </summary>
    (string, bool) ContainerRuntimeIdentifier => runtimeIdentifier switch
    {
        "linux-x64" => ("linux-x64", true),
        "linux-musl-x64" => ("alpine", false),
        _ => throw new ArgumentException($"There is no container image for: {runtimeIdentifier}"),
    };

    private List<string> ContainerTags()
    {
        if (IsLocalBuild)
        {
            return ["local", $"local-{ContainerRuntimeIdentifier.Item1}"];
        }
        List<string> tagsOriginal = [Version, VersionMajorMinor, VersionMajor];
        if (ContainerRuntimeIdentifier.Item2)
        {
            tagsOriginal.Add("latest");
        }
        var tags = tagsOriginal.Select(tag => $"{ContainerRuntimeIdentifier.Item1}-{tag}").ToList();
        if (containerDefaultRID == runtimeIdentifier)
        {
            tags.AddRange(tagsOriginal);
        }

        return tags;
    }
}
