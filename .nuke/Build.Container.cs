using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Cri = (string identifier, string family);

namespace SuCoS.NUKE;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for create the Container (Docker) image.
/// </summary>
partial class Build
{
    [Parameter("GitLab Project CI_REGISTRY_IMAGE")]
    public readonly string ContainerRegistryImage;

    private string RegistryImage => ContainerRegistryImage ?? "sucos";

    [Parameter("GitLab Project Full Address")]
    public readonly string ContainerDefaultRid = "linux-x64";

    /// <summary>
    /// Create the container.
    /// Note that, despite using Publish, it will not rebuild the app, so
    /// "Publish" target should be called before
    /// </summary>
    private Target CreateContainer => td => td
        .OnlyWhenStatic(() => ContainerRuntimeIdentifier is not null)
        .After(Restore)
        .Executes(() =>
        {
            var tags = " \"" + string.Join(";", ContainerTags()) + "\" ";

            // Build the Container image (do not reuse the compiled version from Publish)
            DotNetTasks.DotNetPublish(s => s
                .SetProject(Solution.SuCoS)
                .SetConfiguration(ConfigurationSet)
                .SetOutput(PublishDir)
                .SetRuntime(RuntimeIdentifier)
                .SetSelfContained(PublishSelfContained)
                .SetPublishSingleFile(PublishSingleFile)
                .SetPublishTrimmed(PublishTrimmed)
                .SetPublishReadyToRun(PublishReadyToRun)
                .SetVersion(CurrentVersion)
                .SetAssemblyVersion(CurrentVersion)
                .SetInformationalVersion(CurrentVersion)

                .AddProperty("EnableSdkContainerSupport", true)
                .AddProperty("ContainerAppCommandInstruction", "None")
                .AddProperty("ContainerWorkingDirectory", "/bin")
                .AddProperty("ContainerRuntimeIdentifier", RuntimeIdentifier)
                .AddProperty("ContainerRepository", RegistryImage)
                .AddProperty("ContainerImageTags", tags)
                .AddProperty("ContainerFamily",
                    ContainerRuntimeIdentifier!.Value.family)
                .AddProcessAdditionalArguments("-target:PublishContainer")
            );
        });

    /// <summary>
    /// Return the tuple that contains:
    /// * the proper image tag for a given OS
    /// * the family of the build (used by dotnet publish to determine the base image)
    /// * if the image mush be marked as "latest"
    /// </summary>
    private Cri? ContainerRuntimeIdentifier => RuntimeIdentifier switch
    {
        "linux-x64" => (identifier: "linux-x64", family: "noble-chiseled"),
        "linux-musl-x64" => (identifier: "alpine", family: "alpine"),
        _ => null,
    };

    private List<string> ContainerTags()
    {
        var cri = ContainerRuntimeIdentifier!.Value;
        var localTag = IsLocalBuild ? "local" : string.Empty;

        var tagsDefault = new List<string> { VersionFull, VersionMajorMinor, VersionMajor, string.Empty }
            .Select(tag => string.IsNullOrEmpty(localTag) || string.IsNullOrEmpty(tag)
                ? $"{localTag}{tag}"
                : $"{localTag}-{tag}")
            .ToList();

        var tags = tagsDefault
            .Select(tag => string.IsNullOrEmpty(tag)
                ? cri.identifier
                : $"{tag}-{cri.identifier}")
            .ToList();

        if (ContainerDefaultRid != RuntimeIdentifier)
        {
            return tags.Where(t => !string.IsNullOrEmpty(t)).ToList();
        }

        tagsDefault.Add(localTag + (string.IsNullOrEmpty(localTag) ? "" : "-") + cri.identifier);
        tags.AddRange(tagsDefault);

        return tags.Where(t => !string.IsNullOrEmpty(t)).ToList();
    }
}
