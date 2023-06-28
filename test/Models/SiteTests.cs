using Xunit;
using Moq;
using SuCoS.Models;
using System.Globalization;

namespace SuCoS.Tests;

/// <summary>
/// Unit tests for the Site class.
/// </summary>
public class SiteTests
{
    private readonly Site site;
    private readonly Mock<ISystemClock> systemClockMock;
    readonly string testSite1Path = ".TestSites/01";

    public SiteTests()
    {
        systemClockMock = new Mock<ISystemClock>();
        var testDate = DateTime.Parse("2023-04-01", CultureInfo.InvariantCulture);
        systemClockMock.Setup(c => c.Now).Returns(testDate);
        site = new Site(systemClockMock.Object);
    }

    [Theory]
    [InlineData("test01.md", @"---
Title: Test Content 1
---

Test Content 1
")]
    [InlineData("test02.md", @"---
Title: Test Content 2
---

Test Content 2
")]
    public void Test_ScanAllMarkdownFiles(string fileName, string fileContent)
    {
        site.SourceDirectoryPath = testSite1Path;
        site.ScanAllMarkdownFiles();

        Assert.Contains(site.RawPages, rp => rp.filePath == fileName && rp.content == fileContent);
    }

    [Theory]
    [InlineData("test1", Kind.index, "base", "Test Content 1")]
    [InlineData("test2", Kind.single, "content", "Test Content 2")]
    public void Test_ResetCache(string firstKeyPart, Kind secondKeyPart, string thirdKeyPart, string value)
    {
        var key = (firstKeyPart, secondKeyPart, thirdKeyPart);
        site.baseTemplateCache.Add(key, value);
        site.contentTemplateCache.Add(key, value);
        site.PagesDict.Add("test", new Frontmatter("Test Title", "sourcePath", site, systemClockMock.Object));

        site.ResetCache();

        Assert.Empty(site.baseTemplateCache);
        Assert.Empty(site.contentTemplateCache);
        Assert.Empty(site.PagesDict);
    }
}
