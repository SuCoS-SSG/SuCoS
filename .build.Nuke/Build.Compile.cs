using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace SuCoS;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the build process.
/// </summary>
sealed partial class Build : NukeBuild
{
    [Parameter("output-directory (default: ./output)")]
    readonly string outputDirectory = RootDirectory / "output";

    Target Clean => _ => _
        .Executes(() =>
        {
            sourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(
                (path) => path.DeleteDirectory()
            );
            PublishDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetNoLogo(true)
                .SetProjectFile(solution)
                .SetConfiguration(configuration)
                .SetOutputDirectory(outputDirectory)
                .EnableNoRestore()
                );
        });
}
