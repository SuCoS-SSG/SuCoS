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
sealed partial class Build : NukeBuild
{
    static AbsolutePath testDirectory => RootDirectory / "test";
    static AbsolutePath testDLLDirectory => testDirectory / "bin" / "Debug" / "net8.0";
    static AbsolutePath testAssembly => testDLLDirectory / "test.dll";
    static AbsolutePath coverageDirectory => RootDirectory / "coverage-results";
    static AbsolutePath coverageResultDirectory => coverageDirectory / "coverage";
    static AbsolutePath coverageResultFile => coverageResultDirectory / "coverage.xml";
    static AbsolutePath coverageReportDirectory => coverageDirectory / "report";
    static AbsolutePath coverageReportSummaryDirectory => coverageReportDirectory / "Summary.txt";

    Target Test => td => td
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