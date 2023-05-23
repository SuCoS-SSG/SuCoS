using System;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace SuCoS;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    [Parameter("configuration")]
    readonly Configuration configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution]
    readonly Solution solution;

    [Parameter("Runtime identifier for the build (e.g., win-x64, linux-x64, osx-x64)")]
    readonly string runtimeIdentifier = "linux-x64";

    [Parameter("publish-self-contained")]
    readonly bool publishSelfContained = true;

    [Parameter("publish-single-file")]
    readonly bool publishSingleFile = false;

    [Parameter("publish-trimmed")]
    readonly bool publishTrimmed = false;

    [Parameter("artifacts-directory")]
    readonly AbsolutePath artifactsDirectory = RootDirectory / "publish";

    static AbsolutePath SourceDirectory => RootDirectory / "src";
    static AbsolutePath OutputDirectory => RootDirectory / "output";

    public Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(
                (path) => path.DeleteDirectory()
            );
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(solution));
        });

    public Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetNoLogo(true)
                .SetProjectFile(solution)
                .SetConfiguration(configuration)
                .EnableNoRestore());
        });

    public Target Publish => _ => _
    .DependsOn(Compile)
    .Executes(() =>
    {
        Console.WriteLine(RootDirectory);
        Console.WriteLine(artifactsDirectory);
        DotNetPublish(s => s
            .SetNoLogo(true)
            .SetProject(solution)
            .SetConfiguration(configuration)
            .SetOutput(artifactsDirectory)
            .SetRuntime(runtimeIdentifier)
            .SetSelfContained(publishSelfContained)
            .SetPublishSingleFile(publishSingleFile)
            .SetPublishTrimmed(publishTrimmed)
            .SetAuthors("Bruno Massa")
            );
    });

    static int Main() => Execute<Build>(x => x.Compile);
}
