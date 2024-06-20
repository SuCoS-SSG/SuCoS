using System.Globalization;
using NSubstitute;
using Serilog;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parsers;

namespace test;

public class TestSetup
{
    protected const string TitleConst = "Test Title";
    protected const string SourcePathConst = "/path/to/file.md";
    protected readonly DateTime TodayDate = DateTime.Parse("2023-04-01", CultureInfo.InvariantCulture);
    protected readonly DateTime FutureDate = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);

    protected const string TestSitePathConst01 = ".TestSites/01";
    protected const string TestSitePathConst02 = ".TestSites/02-have-index";
    protected const string TestSitePathConst03 = ".TestSites/03-section";
    protected const string TestSitePathConst04 = ".TestSites/04-tags";
    protected const string TestSitePathConst05 = ".TestSites/05-theme-no-baseof";
    protected const string TestSitePathConst06 = ".TestSites/06-theme";
    protected const string TestSitePathConst07 = ".TestSites/07-theme-no-baseof-error";
    protected const string TestSitePathConst08 = ".TestSites/08-theme-html";
    protected const string TestSitePathConst09 = ".TestSites/09-cascade";

    protected readonly IMetadataParser FrontMatterParser = new YamlParser();
    protected readonly IGenerateOptions GenerateOptionsMock = Substitute.For<IGenerateOptions>();
    private readonly SiteSettings _siteSettingsMock = Substitute.For<SiteSettings>();
    protected readonly ILogger LoggerMock = Substitute.For<ILogger>();
    protected readonly ISystemClock SystemClockMock = Substitute.For<ISystemClock>();
    protected readonly IFrontMatter FrontMatterMock = new FrontMatter
    {
        Title = TitleConst,
        SourceRelativePath = SourcePathConst
    };

    protected ISite Site;

    // based on the compiled test.dll path
    // that is typically "bin/Debug/netX.0/test.dll"
    protected const string TestSitesPath = "../../..";

    protected TestSetup()
    {
        _ = SystemClockMock.Now.Returns(TodayDate);
        Site = new Site(GenerateOptionsMock, _siteSettingsMock, FrontMatterParser, LoggerMock, SystemClockMock);
    }

    public TestSetup(SiteSettings siteSettings)
    {
        _ = SystemClockMock.Now.Returns(TodayDate);
        Site = new Site(GenerateOptionsMock, siteSettings, FrontMatterParser, LoggerMock, SystemClockMock);
    }
}
