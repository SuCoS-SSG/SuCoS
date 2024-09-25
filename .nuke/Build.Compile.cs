using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;

namespace SuCoS.NUKE;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the build process.
/// </summary>
internal sealed partial class Build
{
    private Target Clean => s => s
        .Executes(() =>
        {
            Solution.SuCoS.Directory.GlobDirectories("**/bin", "**/obj", "**/output").ForEach(
                (path) => path.DeleteDirectory()
            );
            Solution.SuCoS_Test.Directory.GlobDirectories("**/bin", "**/obj", "**/output").ForEach(
                (path) => path.DeleteDirectory()
            );
            PublishDir.DeleteDirectory();
            CoverageDirectory.DeleteDirectory();
        });

    private Target Restore => td => td
        .After(Clean)
        .Executes(() =>
        {
            _ = DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    private Target Compile => td => td
        .DependsOn(Restore)
        .Executes(() =>
        {
            Log.Debug("Configuration {Configuration}", ConfigurationSet);
            Log.Debug("configuration {configuration}", Configuration);
            _ = DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(ConfigurationSet)
                .EnableNoRestore()
            );
        });
}
