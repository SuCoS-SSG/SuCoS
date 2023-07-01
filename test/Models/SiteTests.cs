using Xunit;
using Moq;
using SuCoS.Models;
using System.Globalization;
using SuCoS.Helper;

namespace SuCoS.Tests;

/// <summary>
/// Unit tests for the Site class.
/// </summary>
public class SiteTests
{
    private readonly Site site;
    private readonly Mock<ISystemClock> systemClockMock;
    readonly string testSite1Path = ".TestSites/01";

    // based on the compiled test.dll path
    // that is typically "bin/Debug/netX.0/test.dll"
    readonly string testSitesPath = "../../..";

    public SiteTests()
    {
        systemClockMock = new Mock<ISystemClock>();
        var testDate = DateTime.Parse("2023-04-01", CultureInfo.InvariantCulture);
        systemClockMock.Setup(c => c.Now).Returns(testDate);
        site = new Site(systemClockMock.Object);
    }

    [Theory]
    [InlineData("test01.md")]
    [InlineData("test02.md")]
    public void Test_ScanAllMarkdownFiles(string fileName)
    {
        var siteFullPath = Path.GetFullPath(Path.Combine(testSitesPath, testSite1Path));

        // Act
        var ContentPaths = FileUtils.GetAllMarkdownFiles(Path.Combine(siteFullPath, "content"));
        var fileFullPath = Path.Combine(siteFullPath, "content", fileName);

        // Assert
        Assert.Contains(ContentPaths, rp => rp == fileFullPath);
    }

    [Theory]
    [InlineData("test1", Kind.index, "base", "Test Content 1")]
    [InlineData("test2", Kind.single, "content", "Test Content 2")]
    public void Test_ResetCache(string firstKeyPart, Kind secondKeyPart, string thirdKeyPart, string value)
    {
        var key = (firstKeyPart, secondKeyPart, thirdKeyPart);
        site.baseTemplateCache.Add(key, value);
        site.contentTemplateCache.Add(key, value);
        site.PagesDict.Add("test", new Frontmatter("Test Title", "sourcePath", site));

        site.ResetCache();

        Assert.Empty(site.baseTemplateCache);
        Assert.Empty(site.contentTemplateCache);
        Assert.Empty(site.PagesDict);
    }
}
