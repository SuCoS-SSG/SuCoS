using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;

namespace SuCoS.NUKE;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the publish process.
/// </summary>
partial class Build
{
    [Parameter("Runtime identifier for the build (e.g., win-x64, linux-x64, osx-x64) (default: linux-x64)")]
    public readonly string RuntimeIdentifier = "linux-x64";

    [Parameter("publish-directory (default: ./publish/{runtimeIdentifier})")]
    public readonly AbsolutePath PublishDirectory;
    private AbsolutePath PublishDir => PublishDirectory ?? RootDirectory / "publish" / RuntimeIdentifier;

    [Parameter("publish-self-contained (default: true)")]
    public readonly bool PublishSelfContained = true;

    [Parameter("publish-single-file (default: true)")]
    public readonly bool PublishSingleFile = true;

    [Parameter("publish-trimmed (default: false)")]
    public readonly bool PublishTrimmed = true;

    [Parameter("publish-ready-to-run (default: true)")]
    public readonly bool PublishReadyToRun = true;

    private Target Publish => td => td
        .After(Restore)
        .Executes(() =>
        {
            _ = DotNetTasks.DotNetPublish(s => s
                .SetProject(Solution.SuCoS)
                .SetConfiguration(ConfigurationSet)
                .SetOutput(PublishDir)
                .SetRuntime(RuntimeIdentifier)
                .SetSelfContained(PublishSelfContained)
                .SetPublishSingleFile(PublishSingleFile)
                .SetPublishReadyToRun(PublishReadyToRun)
                .SetPublishTrimmed(PublishTrimmed)
                .SetVersion(CurrentVersion)
                .SetAssemblyVersion(CurrentVersion)
                .SetInformationalVersion(CurrentVersion)
                .AddProperty("TrimMode", "partial")
                .AddProperty("EnableTrimAnalyzer", PublishTrimmed)
            );
        });
}
