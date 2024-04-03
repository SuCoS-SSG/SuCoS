using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace SuCoS;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the publish process.
/// </summary>
sealed partial class Build : NukeBuild
{
  [Parameter("Runtime identifier for the build (e.g., win-x64, linux-x64, osx-x64) (default: linux-x64)")]
  readonly string runtimeIdentifier = "linux-x64";

  [Parameter("publish-directory (default: ./publish/{runtimeIdentifier})")]
  readonly AbsolutePath publishDirectory;
  AbsolutePath PublishDirectory => publishDirectory ?? RootDirectory / "publish" / runtimeIdentifier;

  [Parameter("publish-self-contained (default: true)")]
  readonly bool publishSelfContained = true;

  [Parameter("publish-single-file (default: true)")]
  readonly bool publishSingleFile = true;

  [Parameter("publish-trimmed (default: false)")]
  readonly bool publishTrimmed = false;

  Target Publish => td => td
      .After(Restore)
      .Executes(() =>
      {
        _ = DotNetPublish(s => s
            .SetNoLogo(true)
            .SetProject("source/SuCoS.csproj")
            .SetConfiguration(configurationSet)
            .SetOutput(PublishDirectory)
            .SetRuntime(runtimeIdentifier)
            .SetSelfContained(publishSelfContained)
            .SetPublishSingleFile(publishSingleFile)
            .SetPublishTrimmed(publishTrimmed)
            .SetAuthors("Bruno Massa")
            .SetVersion(CurrentVersion)
            .SetAssemblyVersion(CurrentVersion)
            .SetInformationalVersion(CurrentVersion)
            );
      });
}
