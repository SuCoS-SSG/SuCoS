using NSubstitute;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using System.Globalization;
using Xunit;

namespace Tests.Models;

public class PageTests : TestSetup
{
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

    [Theory]
    [InlineData("Test Title", "/path/to/file.md", "file", "/path/to")]
    public void Frontmatter_ShouldCreateWithCorrectProperties(string title, string sourcePath, string sourceFileNameWithoutExtension, string sourcePathDirectory)
    {
        var page = new Page(frontMatterMock, site);

        // Assert
        Assert.Equal(title, page.Title);
        Assert.Equal(sourcePath, page.SourceRelativePath);
        Assert.Same(site, page.Site);
        Assert.Equal(sourceFileNameWithoutExtension, page.SourceFileNameWithoutExtension);
        Assert.Equal(sourcePathDirectory, page.SourceRelativePathDirectory);
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
        Assert.Empty(page.AllOutputURLs);
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
            SourceRelativePath = sourcePathCONST,
            Aliases = new() { "v123", "{{ page.Title }}", "{{ page.Title }}-2" }
        }, site);

        // Act
        site.PostProcessPage(page);

        // Assert
        Assert.Equal(3, site.OutputReferences.Count);
        site.OutputReferences.TryGetValue(url, out var pageOther);
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
            SourceRelativePath = sourcePathCONST,
            ExpiryDate = systemClockMock.Now.AddDays(days)
        }, site);

        // Assert
        Assert.Equal(expected, site.IsDateExpired(page));
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "2024-01-01", false)]
    [InlineData("2022-01-01", null, true)]
    [InlineData("2024-01-01", "2022-01-01", false)]
    [InlineData("2022-01-01", "2024-01-01", true)]
    public void IsDatePublishable_ShouldReturnCorrectValues(string? publishDate, string? date, bool expectedValue)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourceRelativePath = sourcePathCONST,
            PublishDate = publishDate is null ? null : DateTime.Parse(publishDate, CultureInfo.InvariantCulture),
            Date = date is null ? null : DateTime.Parse(date, CultureInfo.InvariantCulture)
        }, site);

        // Assert
        Assert.Equal(expectedValue, site.IsDatePublishable(page));
    }

    [Theory]
    // Draft as null
    [InlineData(null, null, null, false, true)]
    [InlineData(null, "2024-01-01", null, false, false)]
    [InlineData("2022-01-01", null, null, false, true)]
    [InlineData("2024-01-01", "2022-01-01", null, false, false)]
    [InlineData("2022-01-01", "2024-01-01", null, false, true)]
    // Draft as false
    [InlineData(null, null, false, false, true)]
    [InlineData(null, "2024-01-01", false, false, false)]
    [InlineData("2022-01-01", null, false, false, true)]
    [InlineData("2024-01-01", "2022-01-01", false, false, false)]
    [InlineData("2022-01-01", "2024-01-01", false, false, true)]
    // Draft as true
    [InlineData(null, null, true, false, false)]
    [InlineData(null, "2024-01-01", true, false, false)]
    [InlineData("2022-01-01", null, true, false, false)]
    [InlineData("2024-01-01", "2022-01-01", true, false, false)]
    [InlineData("2022-01-01", "2024-01-01", true, false, false)]
    // Draft as null, option -d
    [InlineData(null, null, null, true, true)]
    [InlineData(null, "2024-01-01", null, true, false)]
    [InlineData("2022-01-01", null, null, true, true)]
    [InlineData("2024-01-01", "2022-01-01", null, true, false)]
    [InlineData("2022-01-01", "2024-01-01", null, true, true)]
    // Draft as false, option -d
    [InlineData(null, null, false, true, true)]
    [InlineData(null, "2024-01-01", false, true, false)]
    [InlineData("2022-01-01", null, false, true, true)]
    [InlineData("2024-01-01", "2022-01-01", false, true, false)]
    [InlineData("2022-01-01", "2024-01-01", false, true, true)]
    // Draft as true, option -d
    [InlineData(null, null, true, true, true)]
    [InlineData(null, "2024-01-01", true, true, false)]
    [InlineData("2022-01-01", null, true, true, true)]
    [InlineData("2024-01-01", "2022-01-01", true, true, false)]
    [InlineData("2022-01-01", "2024-01-01", true, true, true)]
    public void IsValidPage_ShouldReturnCorrectValues(string? publishDate, string? date, bool? draft, bool draftOption, bool expectedValue)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourceRelativePath = sourcePathCONST,
            PublishDate = publishDate is null ? null : DateTime.Parse(publishDate, CultureInfo.InvariantCulture),
            Date = date is null ? null : DateTime.Parse(date, CultureInfo.InvariantCulture),
            Draft = draft
        }, site);

        var options = Substitute.For<IGenerateOptions>();
        options.Draft.Returns(draftOption);

        // Assert
        Assert.Equal(expectedValue, site.IsValidPage(page, options));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void IsValidDate_ShouldReturnExpectedResult(bool futureOption, bool expected)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourceRelativePath = sourcePathCONST,
            Date = systemClockMock.Now.AddDays(1)
        }, site);

        // Act
        var options = Substitute.For<IGenerateOptions>();
        options.Future.Returns(futureOption);

        // Assert
        Assert.Equal(expected, site.IsValidDate(page, options));
    }

    [Theory]
    [InlineData("/test/path/index.md", "/test-title")]
    [InlineData("/another/path/index.md", "/test-title")]
    public void CreatePermalink_ShouldReturnCorrectUrl_WhenUrlIsNull(string sourcePath, string expectedUrl)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourceRelativePath = sourcePath
        }, site);

        // Assert
        Assert.Equal(expectedUrl, page.CreatePermalink());
    }

    [Theory]
    [InlineData(null, "/test-title")]
    [InlineData("{{ page.Title }}/{{ page.SourceFileNameWithoutExtension }}", "/test-title/file")]
    public void Permalink_CreateWithDefaultOrCustomURLTemplate(string? urlTemplate, string expectedPermalink)
    {
        var page = new Page(new FrontMatter
        {
            Title = titleCONST,
            SourceRelativePath = sourcePathCONST,
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
        ArgumentException.ThrowIfNullOrEmpty(plain);
        var page = new Page(new FrontMatter
        {
            RawContent = rawContent
        }, site);
        // Required to make the test pass on Windows
        plain = plain.Replace("\r\n", "\n", StringComparison.Ordinal);

        // Assert
        Assert.Equal(plain, page.Plain);
    }
}
