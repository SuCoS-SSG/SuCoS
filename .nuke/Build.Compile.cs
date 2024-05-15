using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;

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
            _ = DotNetTasks.DotNetRestore(s => DotNetRestoreSettingsExtensions
                .SetProjectFile<DotNetRestoreSettings>(s, solution));
        });

    Target Compile => td => td
        .After(Restore)
        .Executes(() =>
        {
            Log.Debug("Configuration {Configuration}", configurationSet);
            Log.Debug("configuration {configuration}", configuration);
            _ = DotNetTasks.DotNetBuild(s => DotNetBuildSettingsExtensions
                .SetNoLogo<DotNetBuildSettings>(s, true)
                .SetProjectFile(solution)
                .SetConfiguration(configurationSet)
                .EnableNoRestore()
            );
        });
}