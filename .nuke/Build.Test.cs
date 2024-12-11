using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Serilog;

namespace SuCoS.NUKE;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the build process.
/// </summary>
internal sealed partial class Build
{
    // private  AbsolutePath TestDllDirectory => Solution.SuCoS_Test.Directory;// / "bin" / "Debug" / "net9.0";
    // private  AbsolutePath TestAssembly => TestDllDirectory / Solution.SuCoS_Test.Name + ".dll";
    private static AbsolutePath CoverageDirectory => RootDirectory / "coverage";
    private static AbsolutePath CoverageResultFile => CoverageDirectory / "coverage.xml";
    private static AbsolutePath CoverageReportDirectory => CoverageDirectory / "report";
    private static AbsolutePath CoverageReportSummaryDirectory => CoverageReportDirectory / "Summary.txt";

    private Target Test => td => td
        .After(Compile)
        .Produces(CoverageResultFile)
        .Executes(() =>
            DotNetTasks.DotNetTest(settings => settings
                    .SetProjectFile(Solution.SuCoS_Test)
                    .SetConfiguration(ConfigurationSet)

                    // Test Coverage
                    .SetResultsDirectory(CoverageDirectory)
                    .SetCoverletOutput(CoverageResultFile)
                    .SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
                    .SetExcludeByFile("**/*.g.cs") // Exclude source generated files
                    .EnableCollectCoverage()
            )
        );

    public Target TestReport => td => td
        .DependsOn(Test)
        .Consumes(Test, CoverageResultFile)
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
