using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.ReportGenerator;
using Serilog;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the build process.
/// </summary>
internal sealed partial class Build : NukeBuild
{
    private static AbsolutePath testDirectory => RootDirectory / "test";
    private static AbsolutePath testDLLDirectory => testDirectory / "bin" / "Debug" / "net8.0";
    private static AbsolutePath testAssembly => testDLLDirectory / "test.dll";
    private static AbsolutePath coverageDirectory => RootDirectory / "coverage-results";
    private static AbsolutePath coverageResultDirectory => coverageDirectory / "coverage";
    private static AbsolutePath coverageResultFile => coverageResultDirectory / "coverage.xml";
    private static AbsolutePath coverageReportDirectory => coverageDirectory / "report";
    private static AbsolutePath coverageReportSummaryDirectory => coverageReportDirectory / "Summary.txt";

    private Target Test => td => td
        .After(Compile)
        .Executes(() =>
        {
            _ = coverageResultDirectory.CreateDirectory();
            _ = CoverletTasks.Coverlet(s => CoverletSettingsExtensions
                    .SetTarget<CoverletSettings>(s, "dotnet")
                    .SetTargetArgs("test --no-build --no-restore")
                    .SetAssembly(testAssembly)
                    // .SetThreshold(75)
                    .SetOutput(coverageResultFile)
                    .SetFormat(CoverletOutputFormat.cobertura)
                    .SetExcludeByFile(["**/*.g.cs"]) // Exclude source generated files
            );
        });

    public Target TestReport => td => td
        .DependsOn(Test)
        .Executes(() =>
        {
            _ = coverageReportDirectory.CreateDirectory();
            _ = ReportGeneratorTasks.ReportGenerator(s => ReportGeneratorSettingsExtensions
                .SetTargetDirectory<ReportGeneratorSettings>(s, coverageReportDirectory)
                .SetReportTypes([ReportTypes.Html, ReportTypes.TextSummary])
                .SetReports(coverageResultFile)
            );
            var summaryText = coverageReportSummaryDirectory.ReadAllLines();
            Log.Information(string.Join(Environment.NewLine, summaryText));
        });
}
