using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the publish debian package.
/// </summary>
internal sealed partial class Build : NukeBuild
{
    private AbsolutePath DebianPackage => PublishDir / "SuCoS.deb";
    private string DebianDistribution = "bookworm";
    private string DebianComponent = "main";

    public Target CreateDebianPackage => _ => _
        .After(Publish)
        .OnlyWhenStatic(() => RuntimeIdentifier == "linux-x64")
        .Executes(() =>
        {
            var DebianPath = Solution._nuke.Directory / "Debian";
            var sucosPath = DebianPath / "usr" / "local" / "bin" / "SuCoS";
            var DebianControlFilePre = DebianPath / "DEBIAN" / "_control";
            var DebianControlFile = DebianPath / "DEBIAN" / "control";
            FileSystemTasks.CopyFile(DebianControlFilePre, DebianControlFile, FileExistsPolicy.Overwrite);
            FileSystemTasks.CopyFile(PublishDir / "SuCoS", sucosPath, FileExistsPolicy.Overwrite);

            var controlContent = DebianControlFile.ReadAllText()
                .Replace("SUCOS_VERSION", VersionFull.ToString(), StringComparison.InvariantCulture);

            DebianControlFile.WriteAllText(controlContent);

            ProcessTasks.StartProcess("dpkg-deb", $"--build {DebianPath} {DebianPackage}")
                .AssertZeroExitCode();
        });
}
