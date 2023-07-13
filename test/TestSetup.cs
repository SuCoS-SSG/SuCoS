using Serilog;
using Moq;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parser;
using System.Globalization;

public class TestSetup
{
    protected const string titleCONST = "Test Title";
    protected const string sourcePathCONST = "/path/to/file.md";
    protected readonly DateTime todayDate = DateTime.Parse("2023-04-01", CultureInfo.InvariantCulture);
    protected readonly DateTime futureDate = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);    

    protected const string testSitePathCONST01 = ".TestSites/01";
    protected const string testSitePathCONST02 = ".TestSites/02-have-index";
    protected const string testSitePathCONST03 = ".TestSites/03-section";
    protected const string testSitePathCONST04 = ".TestSites/04-tags";
    protected const string testSitePathCONST05 = ".TestSites/05-theme-no-baseof";
    protected const string testSitePathCONST06 = ".TestSites/06-theme";
    protected const string testSitePathCONST07 = ".TestSites/07-theme-no-baseof-error";
    protected const string testSitePathCONST08 = ".TestSites/08-theme-html";

    protected readonly IFrontMatterParser frontMatterParser = new YAMLParser();
    protected readonly Mock<IGenerateOptions> generateOptionsMock = new();
    protected readonly Mock<SiteSettings> siteSettingsMock = new();
    protected readonly Mock<ILogger> loggerMock = new();
    protected readonly Mock<ISystemClock> systemClockMock = new();
    protected readonly IFrontMatter frontMatterMock = new FrontMatter()
    {
        Title = titleCONST,
        SourcePath = sourcePathCONST
    };

    protected readonly ISite site;

    // based on the compiled test.dll path
    // that is typically "bin/Debug/netX.0/test.dll"
    protected const string testSitesPath = "../../..";

    public TestSetup()
    {
        systemClockMock.Setup(c => c.Now).Returns(todayDate);
        site = new Site(generateOptionsMock.Object, siteSettingsMock.Object, frontMatterParser, loggerMock.Object, systemClockMock.Object);
    }
}