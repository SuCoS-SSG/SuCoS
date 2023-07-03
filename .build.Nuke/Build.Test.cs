using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.OpenCover;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.IO;
using Nuke.Common.Tools.Coverlet;
using static Nuke.Common.Tools.Coverlet.CoverletTasks;
using Serilog;
using System;

namespace SuCoS;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the build process.
/// </summary>
sealed partial class Build : NukeBuild
{
    AbsolutePath testDirectory => RootDirectory / "test";
    AbsolutePath testDLLDirectory => testDirectory / "bin" / "Debug" / "net7.0";
    AbsolutePath testAssembly => testDLLDirectory / "test.dll";
    AbsolutePath coverageDirectory => RootDirectory / "coverage-results";
    AbsolutePath coverageResultDirectory => coverageDirectory / "coverage";
    AbsolutePath coverageResultFile => coverageResultDirectory / "coverage.xml";
    AbsolutePath coverageReportDirectory => coverageDirectory / "report";
    AbsolutePath coverageReportSummaryDirectory => coverageReportDirectory / "Summary.txt";

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            coverageResultDirectory.CreateDirectory();
            Coverlet(s => s
                .SetTarget("dotnet")
                .SetTargetArgs("test --no-build --no-restore")
                .SetAssembly(testAssembly)
                // .SetThreshold(75)
                .SetOutput(coverageResultFile)
                .SetFormat(CoverletOutputFormat.cobertura));
        });

    Target TestReport => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            coverageReportDirectory.CreateDirectory();
            ReportGenerator(s => s
                    .SetTargetDirectory(coverageReportDirectory)
                    .SetReportTypes(new ReportTypes[] { ReportTypes.Html, ReportTypes.TextSummary })
                    .SetReports(coverageResultFile)
                    );
            var summaryText = coverageReportSummaryDirectory.ReadAllLines();
            Log.Information(string.Join(Environment.NewLine, summaryText));
        });
}
