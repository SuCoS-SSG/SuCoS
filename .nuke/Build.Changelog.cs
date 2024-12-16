using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Serilog;

namespace SuCoS.NUKE;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the updating the Changelog.
/// </summary>
partial class Build
{
    [Parameter("Container image full name")]
    public string RepositoryCompareLink = "https://gitlab.com/sucos/sucos/-/compare/";

    [Parameter("Changelog file")]
    public string ChangelogFile { get; set; } = "CHANGELOG.md";

    private const string UnreleasedSection = "## [Unreleased][]";

    [GeneratedRegex(@"## v\[(\d+\.\d+\.\d+)\]")]
    private static partial Regex VersionRegex();

    private Target UpdateChangelog => td => td
        .DependsOn(CheckNewCommits)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            if (!File.Exists(ChangelogFile))
            {
                throw new FileNotFoundException($"Error: File '{ChangelogFile}' not found.");
            }

            var fileContents = File.ReadAllText(ChangelogFile);

            if (IsVersionAlreadyInChangelog(VersionFull, fileContents))
            {
                throw new InvalidOperationException($"Error: Version '{VersionFull}' already exists in the changelog.");
            }

            var previousVersion = GetPreviousVersion();
            if (previousVersion == VersionFull)
            {
                throw new InvalidOperationException($"Version {VersionFull} is the current one");
            }

            var newVersionSection = $@"{Environment.NewLine}## v[{VersionFull}][] {DateTime.UtcNow:yyyy-MM-dd}{Environment.NewLine}";
            var linkReference = $@"[{VersionFull}]: {GetVersionLink($"v{previousVersion}", $"v{VersionFull}")}{Environment.NewLine}";
            var unreleasedLink = $@"[Unreleased]: {GetVersionLink($"v{VersionFull}", "HEAD")}";

            fileContents = InsertTextAtIndex(fileContents, newVersionSection, UnreleasedSection, UnreleasedSection.Length + 1);
            fileContents = InsertTextAtIndex(fileContents, linkReference, $"[{previousVersion}]:", 0);

            fileContents = UpdateUnreleasedLink(fileContents, unreleasedLink, previousVersion);

            File.WriteAllText(ChangelogFile, fileContents);

            Log.Information("Successfully inserted version '{versionFull}' into the changelog.", VersionFull);
        });

    private bool IsVersionAlreadyInChangelog(string versionFull, string fileContents) =>
        VersionRegex().Matches(fileContents).Any(match => match.Groups[1].Value == versionFull);

    private static string InsertTextAtIndex(string fileContents, string newText, string reference, int charDelta)
    {
        var linkInsertIndex = fileContents.LastIndexOf(reference, StringComparison.CurrentCulture);
        if (linkInsertIndex == -1)
        {
            throw new InvalidOperationException("Could not find the correct position to insert the new text.");
        }

        return fileContents.Insert(linkInsertIndex + charDelta, newText);
    }

    private string UpdateUnreleasedLink(string fileContents, string unreleasedLink, string previousVersion)
    {
        var oldUnreleasedLink = $@"[Unreleased]: {GetVersionLink($"v{previousVersion}", "HEAD")}";
        return fileContents.Replace(oldUnreleasedLink, unreleasedLink, StringComparison.InvariantCulture);
    }

    private string GetPreviousVersion()
    {
        var versionPattern = VersionRegex();
        var fileContents = File.ReadAllText(ChangelogFile);

        var versionMatches = versionPattern.Matches(fileContents);

        if (versionMatches.Count == 0)
        {
            return "0.0.0";
        }

        // Return the first match, which is the most recent version
        return versionMatches[0].Groups[1].ToString();
    }

    private string GetVersionLink(string previousVersion, string currentVersion) =>
        $"{RepositoryCompareLink}{previousVersion}...{currentVersion}";
}
