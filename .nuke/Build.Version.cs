using System.Linq;
using Nuke.Common;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitVersion;
using Serilog;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the versioning using GitVersion.
/// </summary>
internal sealed partial class Build : NukeBuild
{
    [GitVersion] private readonly GitVersion gitVersion;

    /// <summary>
    /// The current version, using GitVersion.
    /// </summary>
    private string Version => gitVersion.MajorMinorPatch;

    private string VersionMajor => $"{gitVersion.Major}";

    private string VersionMajorMinor => $"{gitVersion.Major}.{gitVersion.Minor}";

    /// <summary>
    /// The version in a format that can be used as a tag.
    /// </summary>
    private string TagName => $"v{Version}";

    /// <summary>
    /// Checks if there are new commits since the last tag.
    /// </summary>
    private bool HasNewCommits => gitVersion.CommitsSinceVersionSource != "0";

    private string currentVersion;

    private string CurrentTag
    {
        get
        {
            currentVersion ??= GitTasks.Git("describe --tags --abbrev=0").FirstOrDefault().Text;
            return currentVersion;
        }
    }

    private string CurrentVersion => CurrentTag.TrimStart('v');

    /// <summary>
    /// Prints the current version.
    /// </summary>
    private Target ShowCurrentVersion => _ => _
        .Executes(() =>
        {
            var lastCommmit = GitTasks.Git("log -1").FirstOrDefault().Text;
            var status = GitTasks.Git("status").FirstOrDefault().Text;
            Log.Information("Current version:  {Version}", CurrentVersion);
            Log.Information("Current tag:      {Version}", CurrentTag);
            Log.Information("Next version:     {Version}", Version);
        });

    /// <summary>
    /// Checks if there are new commits since the last tag.
    /// If there are no new commits, the whole publish process is skipped.
    /// </summary>
    private Target CheckNewCommits => _ => _
        .DependsOn(ShowCurrentVersion)
        .Executes(() =>
        {
            Log.Information("Next version:    {Version}", TagName);
            Log.Information("Checking for new commits...");

            // If there are no new commits since the last tag, skip tag creation
            // Nuke will stop here and not execute any of the following targets
            if (HasNewCommits)
            {
                Log.Information($"There are {gitVersion.CommitsSinceVersionSource} new commits since last tag.");
            }
            else
            {
                Log.Information("No new commits since last tag. Skipping tag creation.");
            }
        });
}
