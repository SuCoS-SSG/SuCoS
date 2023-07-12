using System.Globalization;
using Moq;
using SuCoS.Models;
using Xunit;
using Serilog;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parser;

namespace Test.Models;

public class PageTests
{
    private readonly IFrontMatterParser frontMatterParser = new YAMLParser();
    private readonly Mock<IGenerateOptions> generateOptionsMock = new();
    private readonly Mock<SiteSettings> siteSettingsMock = new();
    private readonly Mock<ILogger> loggerMock = new();
    private readonly Mock<ISystemClock> systemClockMock = new();
    private readonly FrontMatter frontMatterMock = new()
    {
        Title = titleCONST,
        SourcePath = sourcePathCONST
    };
    private readonly Site site;
    private const string titleCONST = "Test Title";
    private const string sourcePathCONST = "/path/to/file.md";
    private const string markdown1CONST = @"
# word01 word02

word03 word04 word05 6 7 eight

## nine

```cs
console.WriteLine('hello word')
```";
    private const string markdown2CONST = @"
# word01 word02

word03 word04 word05 6 7 [eight](http://example.com)";
    private const string markdownPlain1CONST = @"word01 word02
word03 word04 word05 6 7 eight
nine
console.WriteLine('hello word')
";
    private const string markdownPlain2CONST = @"word01 word02
word03 word04 word05 6 7 eight
";

    public PageTests()
    {
        var testDate = DateTime.Parse("2023-04-01", CultureInfo.InvariantCulture);
        systemClockMock.Setup(c => c.Now).Returns(testDate);
        site = new Site(generateOptionsMock.Object, siteSettingsMock.Object, frontMatterParser, loggerMock.Object, systemClockMock.Object);
    }

    [Theory]
    [InlineData("Test Title", "/path/to/file.md", "file", "/path/to")]
    public void Frontmatter_ShouldCreateWithCorrectProperties(string title, string sourcePath, string sourceFileNameWithoutExtension, string sourcePathDirectory)
    {
        var page = new Page(frontMatterMock, site);

        // Assert
        Assert.Equal(title, page.Title);
        Assert.Equal(sourcePath, page.SourcePath);
        Assert.Same(site, page.Site);
        Assert.Equal(sourceFileNameWithoutExtension, page.SourceFileNameWithoutExtension);
        Assert.Equal(sourcePathDirectory, page.SourcePathDirectory);
    }

    [Fact]
    public void Frontmatter_ShouldHaveDefaultValuesForOptionalProperties()
    {
        // Arrange
        var page = new Page(frontMatterMock, site);

        // Assert
        Assert.Equal(string.Empty, page.Section);
        Assert.Equal(Kind.single, page.Kind);
        Assert.Equal("page", page.Type);
        Assert.Null(page.URL);
        Assert.Empty(page.Params);
        Assert.Null(page.Date);
        Assert.Null(page.LastMod);
        Assert.Null(page.PublishDate);
        Assert.Null(page.ExpiryDate);
        Assert.Null(page.AliasesProcessed);
        Assert.Null(page.Permalink);
        Assert.Empty(page.Urls);
        Assert.Equal(string.Empty, page.RawContent);
        Assert.Empty(page.TagsReference);
        Assert.Empty(page.PagesReferences);
        Assert.Empty(page.RegularPages);
        Assert.False(site.IsDateExpired(page));
        Assert.True(site.IsDatePublishable(page));
    }

    [Theory]
    [InlineData("/v123")]
    [InlineData("/test-title-2")]
    public void Aliases_ShouldParseAsUrls(string url)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourcePath = sourcePathCONST,
            Aliases = new() { "v123", "{{ page.Title }}", "{{ page.Title }}-2" }
        }, site);

        // Act
        site.PostProcessPage(page);

        // Assert
        Assert.Equal(3, site.PagesReferences.Count);
        site.PagesReferences.TryGetValue(url, out var pageOther);
        Assert.NotNull(pageOther);
        Assert.Same(page, pageOther);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    public void IsDateExpired_ShouldReturnExpectedResult(int days, bool expected)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourcePath = sourcePathCONST,
            ExpiryDate = systemClockMock.Object.Now.AddDays(days)
        }, site);

        // Assert
        Assert.Equal(expected, site.IsDateExpired(page));
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "2024-06-28", false)]
    [InlineData("2022-06-28", null, true)]
    [InlineData("2024-06-28", "2022-06-28", false)]
    [InlineData("2022-06-28", "2024-06-28", true)]
    public void IsDatePublishable_ShouldReturnCorrectValues(string? publishDate, string? date, bool expectedValue)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourcePath = sourcePathCONST,
            PublishDate = publishDate is null ? null : DateTime.Parse(publishDate, CultureInfo.InvariantCulture),
            Date = date is null ? null : DateTime.Parse(date, CultureInfo.InvariantCulture)
        }, site);

        // Assert
        Assert.Equal(expectedValue, site.IsDatePublishable(page));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void IsValidDate_ShouldReturnExpectedResult(bool futureOption, bool expected)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourcePath = sourcePathCONST,
            Date = systemClockMock.Object.Now.AddDays(1)
        }, site);

        // Act
        var options = new Mock<IGenerateOptions>();
        options.Setup(o => o.Future).Returns(futureOption);

        // Assert
        Assert.Equal(expected, site.IsValidDate(page, options.Object));
    }

    [Theory]
    [InlineData("/test/path/index.md", "/test-title")]
    [InlineData("/another/path/index.md", "/test-title")]
    public void CreatePermalink_ShouldReturnCorrectUrl_WhenUrlIsNull(string sourcePath, string expectedUrl)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourcePath = sourcePath
        }, site);

        // Assert
        Assert.Equal(expectedUrl, page.CreatePermalink());
    }

    [Theory]
    [InlineData(null, "/test-title")]
    [InlineData("{{ page.Title }}/{{ page.SourceFileNameWithoutExtension }}", "/test-title/file")]
    public void Permalink_CreateWithDefaultOrCustomURLTemplate(string urlTemplate, string expectedPermalink)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourcePath = sourcePathCONST,
            URL = urlTemplate
        }, site);
        var actualPermalink = page.CreatePermalink();

        // Assert
        Assert.Equal(expectedPermalink, actualPermalink);
    }

    [Theory]
    [InlineData(Kind.single, true)]
    [InlineData(Kind.list, false)]
    public void RegularPages_ShouldReturnCorrectPages_WhenKindIsSingle(Kind kind, bool isExpectedPage)
    {
        var page = new Page(frontMatterMock, site) { Kind = kind };

        // Act
        site.PostProcessPage(page);

        // Assert
        Assert.Equal(isExpectedPage, site.RegularPages.Contains(page));
    }

    [Theory]
    [InlineData(markdown1CONST, 13)]
    [InlineData(markdown2CONST, 8)]
    public void WordCount_ShouldReturnCorrectCounts(string rawContent, int wordCountExpected)
    {
        var page = new Page(new FrontMatter
        {
            RawContent = rawContent
        }, site);

        // Assert
        Assert.Equal(wordCountExpected, page.WordCount);
    }

    [Theory]
    [InlineData(markdown1CONST, markdownPlain1CONST)]
    [InlineData(markdown2CONST, markdownPlain2CONST)]
    public void Plain_ShouldReturnCorrectPlainString(string rawContent, string plain)
    {
        var page = new Page(new FrontMatter
        {
            RawContent = rawContent
        }, site);

        // Assert
        Assert.Equal(plain, page.Plain);
    }
}
