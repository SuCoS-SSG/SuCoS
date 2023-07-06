using Xunit;
using Moq;
using System.Globalization;
using SuCoS.Helpers;
using SuCoS.Models;

namespace Test.Models;

/// <summary>
/// Unit tests for the Site class.
/// </summary>
public class SiteTests
{
    private readonly Site site;
    private readonly Mock<ISystemClock> systemClockMock = new();
    private const string testSite1PathCONST = ".TestSites/01";

    // based on the compiled test.dll path
    // that is typically "bin/Debug/netX.0/test.dll"
    private const string testSitesPath = "../../..";

    public SiteTests()
    {
        var testDate = DateTime.Parse("2023-04-01", CultureInfo.InvariantCulture);
        systemClockMock.Setup(c => c.Now).Returns(testDate);
        site = new Site(systemClockMock.Object);
    }

    [Theory]
    [InlineData("test01.md")]
    [InlineData("test02.md")]
    public void Test_ScanAllMarkdownFiles(string fileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var siteFullPath = Path.GetFullPath(Path.Combine(testSitesPath, testSite1PathCONST));

        // Act
        site.ParseAndScanSourceFiles(siteFullPath);

        // Assert
        Assert.Contains(site.Pages, page => page.SourcePathDirectory?.Length == 0);
        Assert.Contains(site.Pages, page => page.SourceFileNameWithoutExtension == fileNameWithoutExtension);
    }

    [Theory]
    [InlineData("test1", Kind.index, "base", "Test Content 1")]
    [InlineData("test2", Kind.single, "content", "Test Content 2")]
    public void Test_ResetCache(string firstKeyPart, Kind secondKeyPart, string thirdKeyPart, string value)
    {
        var key = (firstKeyPart, secondKeyPart, thirdKeyPart);
        site.baseTemplateCache.Add(key, value);
        site.contentTemplateCache.Add(key, value);
        site.PagesReferences.Add("test", new Frontmatter("Test Title", "sourcePath", site));

        site.ResetCache();

        Assert.Empty(site.baseTemplateCache);
        Assert.Empty(site.contentTemplateCache);
        Assert.Empty(site.PagesReferences);
    }
}
