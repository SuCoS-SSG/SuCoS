using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the solution-wide variables.
/// </summary>
internal sealed partial class Build : NukeBuild
{
    [Parameter(
        "Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    private readonly string Configuration;

    private string ConfigurationSet => Configuration ??
                                       (IsLocalBuild
                                           ? global::Configuration.Debug
                                           : global::Configuration.Release);

    [Solution(GenerateProjects = true)] private readonly Solution Solution;

    private static AbsolutePath SourceDirectory => RootDirectory / "source";
}
