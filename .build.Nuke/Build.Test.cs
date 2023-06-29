using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.OpenCover;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.IO;
using Nuke.Common.Tools.Coverlet;
using static Nuke.Common.Tools.Coverlet.CoverletTasks;
using static Nuke.Common.IO.FileSystemTasks;
using Serilog;
using System;

namespace SuCoS;

/// <summary>
/// This is the main build file for the project.
/// This partial is responsible for the build process.
/// </summary>
sealed partial class Build : NukeBuild
{
    AbsolutePath TestDLL => testDirectory / "bin" / "Debug" / "net7.0";
    AbsolutePath testDirectory => RootDirectory / "test";
    AbsolutePath TestSiteDirectory => RootDirectory / "test" / ".TestSites";
    AbsolutePath TestOutputDirectory => TestDLL / ".TestSites";
    AbsolutePath coverageDirectory => RootDirectory / "coverage-results";
    AbsolutePath ReportDirectory => coverageDirectory / "report";
    AbsolutePath CoverageResultDirectory => coverageDirectory / "coverage";
    AbsolutePath CoverageResultFile => CoverageResultDirectory / "coverage.xml";
    AbsolutePath CoverageSummaryResultFile => ReportDirectory / "Summary.txt";

    Target PrepareTestFiles => _ => _
        .After(Clean)
        .Executes(() =>
        {
            TestOutputDirectory.CreateOrCleanDirectory();
            CopyDirectoryRecursively(TestSiteDirectory, TestOutputDirectory, DirectoryExistsPolicy.Merge);
        });

    Target Test => _ => _
        .DependsOn(Compile, PrepareTestFiles)
        .Executes(() =>
        {
            CoverageResultDirectory.CreateDirectory();
            Coverlet(s => s
                .SetTarget("dotnet")
                .SetTargetArgs("test --no-build --no-restore")
                .SetAssembly(TestDLL / "test.dll")
                // .SetThreshold(75)
                .SetOutput(CoverageResultFile)
                .SetFormat(CoverletOutputFormat.opencover));
        });

    public Target TestReport => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            ReportDirectory.CreateDirectory();
            ReportGenerator(s => s
                    .SetTargetDirectory(ReportDirectory)
                    .SetReportTypes(new ReportTypes[] { ReportTypes.Html, ReportTypes.TextSummary })
                    .SetReports(CoverageResultFile)
                    );
            var summaryText = CoverageSummaryResultFile.ReadAllLines();
            Log.Information(string.Join(Environment.NewLine, summaryText));
        });
}
