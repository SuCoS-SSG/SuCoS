using Nuke.Common;
using Nuke.Common.CI;

namespace SuCoS;

/// <summary>
/// This is the main build file for the project.
/// </summary>
[ShutdownDotNetAfterServerBuild]
sealed partial class Build : NukeBuild
{
    static int Main() => Execute<Build>(x => x.Compile);
}
