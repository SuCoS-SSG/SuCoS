using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace SuCoS.Nuke;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the build process.
/// </summary>
sealed partial class Build : NukeBuild
{
    Target Clean => _ => _
        .Executes(() =>
        {
            sourceDirectory.GlobDirectories("**/bin", "**/obj", "**/output").ForEach(
                (path) => path.DeleteDirectory()
            );
            testDirectory.GlobDirectories("**/bin", "**/obj", "**/output").ForEach(
                (path) => path.DeleteDirectory()
            );
            PublishDirectory.DeleteDirectory();
            coverageDirectory.DeleteDirectory();
        });

    Target Restore => td => td
        .After(Clean)
        .Executes(() =>
        {
            _ = DotNetRestore(s => s
                .SetProjectFile(solution));
        });

    Target Compile => td => td
        .After(Restore)
        .Executes(() =>
        {
            Log.Debug("Configuration {Configuration}", configurationSet);
            Log.Debug("configuration {configuration}", configuration);
            _ = DotNetBuild(s => s
                .SetNoLogo(true)
                .SetProjectFile(solution)
                .SetConfiguration(configurationSet)
                .EnableNoRestore()
                );
        });
}
