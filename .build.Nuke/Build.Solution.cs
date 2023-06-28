using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace SuCoS;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the solution-wide variables.
/// </summary>
sealed partial class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string configuration;
    string configurationSet => configuration ?? (IsLocalBuild ? Configuration.Debug : Configuration.Release);

    [Solution]
    readonly Solution solution;

    static AbsolutePath sourceDirectory => RootDirectory / "source";
}
