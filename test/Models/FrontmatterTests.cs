using System.Globalization;
using Moq;
using SuCoS.Models;
using Xunit;

namespace SuCoS.Tests;

public class FrontmatterTests
{
    private readonly ISystemClock clock;
    private readonly Mock<ISystemClock> systemClockMock;
    private readonly string title = "Test Title";
    private readonly string sourcePath = "/path/to/file.md";
    private readonly Site site;

    public FrontmatterTests()
    {
        systemClockMock = new Mock<ISystemClock>();
        var testDate = DateTime.Parse("2023-04-01", CultureInfo.InvariantCulture);
        systemClockMock.Setup(c => c.Now).Returns(testDate);

        clock = systemClockMock.Object;
        site = new(clock);
    }

    [Theory]
    [InlineData("Test Title", "/path/to/file.md", "file", "/path/to")]
    public void ShouldCreateFrontmatterWithCorrectProperties(string title, string sourcePath, string sourceFileNameWithoutExtension, string sourcePathDirectory)
    {
        var frontmatter = new Frontmatter(title, sourcePath, site, sourceFileNameWithoutExtension, sourcePathDirectory);

        Assert.Equal(title, frontmatter.Title);
        Assert.Equal(sourcePath, frontmatter.SourcePath);
        Assert.Same(site, frontmatter.Site);
        Assert.Equal(sourceFileNameWithoutExtension, frontmatter.SourceFileNameWithoutExtension);
        Assert.Equal(sourcePathDirectory, frontmatter.SourcePathDirectory);
    }

    [Fact]
    public void ShouldHaveDefaultValuesForOptionalProperties()
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
    public void ShouldReturnValidDateBasedOnExpiryDateAndPublishDate()
    {
        var publishDate = new DateTime(2023, 6, 1);
        var expiryDate = new DateTime(2023, 6, 3);

        systemClockMock.Setup(c => c.Now).Returns(new DateTime(2023, 6, 2));

        var frontmatter = new Frontmatter(title, sourcePath, site)
        {
            ExpiryDate = expiryDate,
            PublishDate = publishDate
        };

        Assert.True(frontmatter.IsValidDate(null));
    }

    [Theory]
    [InlineData(null, "/path/to/test-title")]
    [InlineData("{{ page.Title }}/{{ page.SourceFileNameWithoutExtension }}", "/test-title/file")]
    public void ShouldCreatePermalinkWithDefaultOrCustomURLTemplate(string urlTemplate, string expectedPermalink)
    {
        var frontmatter = new Frontmatter(title, sourcePath, site)
        {
            URL = urlTemplate
        };

        var actualPermalink = frontmatter.CreatePermalink();

        Assert.Equal(expectedPermalink, actualPermalink);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    public void IsDateExpired_ShouldReturnExpectedResult(int days, bool expected)
    {
        systemClockMock.Setup(c => c.Now).Returns(new DateTime(2023, 6, 28));

        var frontmatter = new Frontmatter(title, sourcePath, site)
        {
            ExpiryDate = clock.Now.AddDays(days)
        };

        Assert.Equal(expected, frontmatter.IsDateExpired);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void IsValidDate_ShouldReturnExpectedResult(bool futureOption, bool expected)
    {
        systemClockMock.Setup(c => c.Now).Returns(new DateTime(2023, 6, 28));

        var frontmatter = new Frontmatter(title, sourcePath, site)
        {
            Date = clock.Now.AddDays(1)
        };

        var options = new Mock<IGenerateOptions>();
        options.Setup(o => o.Future).Returns(futureOption);

        Assert.Equal(expected, frontmatter.IsValidDate(options.Object));
    }

    [Theory]
    [InlineData("/test/path", "/test/path/test-title")]
    [InlineData("/another/path", "/another/path/test-title")]
    public void CreatePermalink_ShouldReturnCorrectUrl_WhenUrlIsNull(string sourcePathDirectory, string expectedUrl)
    {
        var frontmatter = new Frontmatter(title, sourcePath, site)
        {
            SourcePathDirectory = sourcePathDirectory
        };

        Assert.Equal(expectedUrl, frontmatter.CreatePermalink());
    }

    [Theory]
    [InlineData(Kind.single, true)]
    [InlineData(Kind.list, false)]
    public void RegularPages_ShouldReturnCorrectPages_WhenKindIsSingle(Kind kind, bool isExpectedPage)
    {
        var page = new Frontmatter(title, sourcePath, site) { Kind = kind };
        site.PostProcessFrontMatter(page);

        Assert.Equal(isExpectedPage, site.RegularPages.Contains(page));
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "2024-06-28", false)]
    [InlineData("2022-06-28", null, true)]
    [InlineData("2024-06-28", "2022-06-28", false)]
    [InlineData("2022-06-28", "2024-06-28", true)]
    public void IsDatePublishable_ShouldReturnCorrectValues(string? publishDate, string? date, bool expectedValue)
    {
        var frontmatter = new Frontmatter(title, sourcePath, site)
        {
            PublishDate = publishDate is null ? null : DateTime.Parse(publishDate, CultureInfo.InvariantCulture),
            Date = date is null ? null : DateTime.Parse(date, CultureInfo.InvariantCulture)
        };

        Assert.Equal(expectedValue, frontmatter.IsDatePublishable);
    }
}
