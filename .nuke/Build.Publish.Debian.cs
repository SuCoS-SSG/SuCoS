using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the publish debian package.
/// </summary>
internal sealed partial class Build
{
    private AbsolutePath DebianPackage => PublishDir / "SuCoS.deb";
    private readonly string DebianDistribution = "bookworm";
    private readonly string DebianComponent = "main";

    public Target CreateDebianPackage => td => td
        .After(Publish)
        .OnlyWhenStatic(() => RuntimeIdentifier == "linux-x64")
        .Executes(() =>
        {
            var debianPath = Solution._nuke.Directory / "Debian";
            var sucosPath = debianPath / "usr" / "local" / "bin" / "SuCoS";
            var debianControlFilePre = debianPath / "DEBIAN" / "_control";
            var debianControlFile = debianPath / "DEBIAN" / "control";
            FileSystemTasks.CopyFile(debianControlFilePre, debianControlFile, FileExistsPolicy.Overwrite);
            FileSystemTasks.CopyFile(PublishDir / "SuCoS", sucosPath, FileExistsPolicy.Overwrite);

            var controlContent = debianControlFile.ReadAllText()
                .Replace("SUCOS_VERSION", VersionFull, StringComparison.InvariantCulture);

            debianControlFile.WriteAllText(controlContent);

            ProcessTasks.StartProcess("dpkg-deb", $"--build {debianPath} {DebianPackage}")
                .AssertZeroExitCode();
        });
}
