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
internal sealed partial class Build
{
    private  AbsolutePath TestDllDirectory => Solution.SuCoS_Test.Directory / "bin" / "Debug" / "net8.0";
    private  AbsolutePath TestAssembly => TestDllDirectory / Solution.SuCoS_Test.Name + ".dll";
    private static AbsolutePath CoverageDirectory => RootDirectory / "coverage-results";
    private static AbsolutePath CoverageResultDirectory => CoverageDirectory / "coverage";
    private static AbsolutePath CoverageResultFile => CoverageResultDirectory / "coverage.xml";
    private static AbsolutePath CoverageReportDirectory => CoverageDirectory / "report";
    private static AbsolutePath CoverageReportSummaryDirectory => CoverageReportDirectory / "Summary.txt";

    private Target Test => td => td
        .After(Compile)
        .Executes(() =>
        {
            _ = CoverageResultDirectory.CreateDirectory();
            _ = CoverletTasks.Coverlet(s => s
                    .SetTarget("dotnet")
                    .SetTargetArgs("test --no-build --no-restore")
                    .SetAssembly(TestAssembly)
                    // .SetThreshold(75)
                    .SetOutput(CoverageResultFile)
                    .SetFormat(CoverletOutputFormat.cobertura)
                    .SetExcludeByFile(["**/*.g.cs"]) // Exclude source generated files
            );
        });

    public Target TestReport => td => td
        .DependsOn(Test)
        .Executes(() =>
        {
            _ = CoverageReportDirectory.CreateDirectory();
            _ = ReportGeneratorTasks.ReportGenerator(s => s
                .SetTargetDirectory(CoverageReportDirectory)
                .SetReportTypes(ReportTypes.Html, ReportTypes.TextSummary)
                .SetReports(CoverageResultFile)
            );
            var summaryText = CoverageReportSummaryDirectory.ReadAllLines();
            Log.Information(string.Join(Environment.NewLine, summaryText));
        });
}
