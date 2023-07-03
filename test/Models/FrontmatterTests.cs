using System.Globalization;
using Moq;
using SuCoS;
using SuCoS.Models;
using Xunit;

namespace Test.Models;

public class FrontmatterTests
{
    private readonly ISystemClock clock;
    private readonly Mock<ISystemClock> systemClockMock = new();
    private readonly Site site;
    private const string titleCONST = "Test Title";
    private const string sourcePathCONST = "/path/to/file.md";

    public FrontmatterTests()
    {
        var testDate = DateTime.Parse("2023-04-01", CultureInfo.InvariantCulture);
        systemClockMock.Setup(c => c.Now).Returns(testDate);
        clock = systemClockMock.Object;
        site = new(clock);
    }

    [Theory]
    [InlineData("Test Title", "/path/to/file.md", "file", "/path/to")]
    public void Frontmatter_ShouldCreateWithCorrectProperties(string title, string sourcePath, string sourceFileNameWithoutExtension, string sourcePathDirectory)
    {
        var frontmatter = new Frontmatter(title, sourcePath, site, sourceFileNameWithoutExtension, sourcePathDirectory);

        // Assert
        Assert.Equal(title, frontmatter.Title);
        Assert.Equal(sourcePath, frontmatter.SourcePath);
        Assert.Same(site, frontmatter.Site);
        Assert.Equal(sourceFileNameWithoutExtension, frontmatter.SourceFileNameWithoutExtension);
        Assert.Equal(sourcePathDirectory, frontmatter.SourcePathDirectory);
    }

    [Fact]
    public void Frontmatter_ShouldHaveDefaultValuesForOptionalProperties()
    {
        // Arrange
        var frontmatter = new Frontmatter("Test Title", "/path/to/file.md", site);

        // Assert
        Assert.Equal(string.Empty, frontmatter.Section);
        Assert.Equal(Kind.single, frontmatter.Kind);
        Assert.Equal("page", frontmatter.Type);
        Assert.Null(frontmatter.URL);
        Assert.Empty(frontmatter.Params);
        Assert.Null(frontmatter.Date);
        Assert.Null(frontmatter.LastMod);
        Assert.Null(frontmatter.PublishDate);
        Assert.Null(frontmatter.ExpiryDate);
        Assert.Null(frontmatter.AliasesProcessed);
        Assert.Null(frontmatter.Permalink);
        Assert.Empty(frontmatter.Urls);
        Assert.Equal(string.Empty, frontmatter.RawContent);
        Assert.Null(frontmatter.Tags);
        Assert.Null(frontmatter.PagesReferences);
        Assert.Empty(frontmatter.RegularPages);
        Assert.False(frontmatter.IsDateExpired);
        Assert.True(frontmatter.IsDatePublishable);
    }

    [Fact]
    public void Aliases_ShouldParseAsUrls()
    {
        var frontmatter = new Frontmatter(titleCONST, sourcePathCONST, site)
        {
            Title = "Title",
            Aliases = new() { "v123", "{{ page.Title }}" }
        };

        // Act
        site.PostProcessFrontMatter(frontmatter);

        // Assert
        foreach (var url in new[] { "/v123", "/title" })
        {
            site.PagesDict.TryGetValue(url, out var frontmatter1);
            Assert.NotNull(frontmatter1);
            Assert.Same(frontmatter, frontmatter1);
        }
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    public void IsDateExpired_ShouldReturnExpectedResult(int days, bool expected)
    {
        var frontmatter = new Frontmatter(titleCONST, sourcePathCONST, site)
        {
            ExpiryDate = clock.Now.AddDays(days)
        };

        // Assert
        Assert.Equal(expected, frontmatter.IsDateExpired);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "2024-06-28", false)]
    [InlineData("2022-06-28", null, true)]
    [InlineData("2024-06-28", "2022-06-28", false)]
    [InlineData("2022-06-28", "2024-06-28", true)]
    public void IsDatePublishable_ShouldReturnCorrectValues(string? publishDate, string? date, bool expectedValue)
    {
        var frontmatter = new Frontmatter(titleCONST, sourcePathCONST, site)
        {
            PublishDate = publishDate is null ? null : DateTime.Parse(publishDate, CultureInfo.InvariantCulture),
            Date = date is null ? null : DateTime.Parse(date, CultureInfo.InvariantCulture)
        };

        // Assert
        Assert.Equal(expectedValue, frontmatter.IsDatePublishable);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void IsValidDate_ShouldReturnExpectedResult(bool futureOption, bool expected)
    {
        var frontmatter = new Frontmatter(titleCONST, sourcePathCONST, site)
        {
            Date = clock.Now.AddDays(1)
        };

        // Act
        var options = new Mock<IGenerateOptions>();
        options.Setup(o => o.Future).Returns(futureOption);

        // Assert
        Assert.Equal(expected, frontmatter.IsValidDate(options.Object));
    }

    [Theory]
    [InlineData("/test/path", "/test/path/test-title")]
    [InlineData("/another/path", "/another/path/test-title")]
    public void CreatePermalink_ShouldReturnCorrectUrl_WhenUrlIsNull(string sourcePathDirectory, string expectedUrl)
    {
        var frontmatter = new Frontmatter(titleCONST, sourcePathCONST, site)
        {
            SourcePathDirectory = sourcePathDirectory
        };

        // Assert
        Assert.Equal(expectedUrl, frontmatter.CreatePermalink());
    }

    [Theory]
    [InlineData(null, "/path/to/test-title")]
    [InlineData("{{ page.Title }}/{{ page.SourceFileNameWithoutExtension }}", "/test-title/file")]
    public void Permalink_CreateWithDefaultOrCustomURLTemplate(string urlTemplate, string expectedPermalink)
    {
        var frontmatter = new Frontmatter(titleCONST, sourcePathCONST, site)
        {
            URL = urlTemplate
        };
        var actualPermalink = frontmatter.CreatePermalink();

        // Assert
        Assert.Equal(expectedPermalink, actualPermalink);
    }

    [Theory]
    [InlineData(Kind.single, true)]
    [InlineData(Kind.list, false)]
    public void RegularPages_ShouldReturnCorrectPages_WhenKindIsSingle(Kind kind, bool isExpectedPage)
    {
        var page = new Frontmatter(titleCONST, sourcePathCONST, site) { Kind = kind };

        // Act
        site.PostProcessFrontMatter(page);

        // Assert
        Assert.Equal(isExpectedPage, site.RegularPages.Contains(page));
    }
}
