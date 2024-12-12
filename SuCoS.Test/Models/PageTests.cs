using System.Globalization;
using NSubstitute;
using SuCoS.Helpers;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using Xunit;

namespace test.Models;

public class PageTests : TestSetup
{
    private const string Markdown1Const = """
                                          # word01 word02

                                          word03 word04 word05 6 7 eight

                                          ## nine

                                          ```cs
                                          console.WriteLine('hello word')
                                          ```
                                          """;

    private const string Markdown2Const = """
                                          # word01 word02

                                          word03 word04 word05 6 7 [eight](https://example.com)
                                          """;

    private const string MarkdownPlain1Const = """
                                               word01 word02
                                               word03 word04 word05 6 7 eight
                                               nine
                                               console.WriteLine('hello word')

                                               """;

    private const string MarkdownPlain2Const = """
                                               word01 word02
                                               word03 word04 word05 6 7 eight

                                               """;

    [Theory]
    [InlineData(TitleConst, SourcePathConst, "file", "/path/to")]
    public void FrontMatter_ShouldCreateWithCorrectProperties(string title,
        string sourcePath, string sourceFileNameWithoutExtension,
        string sourcePathDirectory)
    {
        var page = new Page(ContentSourceMock, Site, "html", []);

        // Assert
        Assert.Equal(title, page.Title);
        Assert.Equal(sourcePath, page.SourceRelativePath);
        Assert.Same(Site, page.Site);
        Assert.Equal(sourceFileNameWithoutExtension,
            page.SourceFileNameWithoutExtension);
        Assert.Equal(sourcePathDirectory, page.SourceRelativePathDirectory);
    }

    [Fact]
    public void FrontMatter_ShouldHaveDefaultValuesForOptionalProperties()
    {
        // Arrange
        var page = new Page(ContentSourceMock, Site, "html", []);

        // Assert
        Assert.Equal(string.Empty, page.Section);
        Assert.Equal(Kind.single, page.Kind);
        Assert.Equal("page", page.Type);
        Assert.Null(page.Url);
        Assert.Empty(page.Params);
        Assert.Null(page.Date);
        Assert.Null(page.LastMod);
        Assert.Null(page.PublishDate);
        Assert.Null(page.ExpiryDate);
        Assert.Null(page.AliasesProcessed);
        Assert.Null(page.Permalink);
        Assert.Empty(page.AllOutputUrLs);
        Assert.Equal(string.Empty, page.RawContent);
        Assert.Empty(page.TagsReference);
        Assert.Empty(page.PagesReferences);
        Assert.Empty(page.RegularPages);
        Assert.False(Site.IsDateExpired(page));
        Assert.True(Site.IsDatePublishable(page));
    }

    [Theory]
    [InlineData("/v123/index.html")]
    [InlineData("/test-title-2/index.html")]
    public void Aliases_ShouldParseAsUrls(string url)
    {
        var page = new Page(new(SourcePathConst, new FrontMatter
        {
            Title = TitleConst,
            Aliases = ["v123", "{{ page.Title }}", "{{ page.Title }}-2"]
        }), Site, "html", []);

        // Act
        Site.PostProcessPage(page);

        // Assert
        Assert.Equal(3, Site.OutputReferences.Count);
        _ = Site.OutputReferences.TryGetValue(url, out var pageOther);
        Assert.NotNull(pageOther);
        Assert.Same(page, pageOther);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(1, false)]
    public void IsDateExpired_ShouldReturnExpectedResult(int days,
        bool expected)
    {
        var page = new Page(new(SourcePathConst, new FrontMatter
        {
            Title = TitleConst,
            ExpiryDate = SystemClockMock.Now.AddDays(days)
        }), Site, "html", []);

        // Assert
        Assert.Equal(expected, Site.IsDateExpired(page));
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "2024-01-01", false)]
    [InlineData("2022-01-01", null, true)]
    [InlineData("2024-01-01", "2022-01-01", false)]
    [InlineData("2022-01-01", "2024-01-01", true)]
    public void IsDatePublishable_ShouldReturnCorrectValues(string? publishDate,
        string? date, bool expectedValue)
    {
        var page = new Page(new(SourcePathConst, new FrontMatter
        {
            Title = TitleConst,
            PublishDate = publishDate is null
                ? null
                : DateTime.Parse(publishDate, CultureInfo.InvariantCulture),
            Date = date is null
                ? null
                : DateTime.Parse(date, CultureInfo.InvariantCulture)
        }), Site, "html", []);

        // Assert
        Assert.Equal(expectedValue, Site.IsDatePublishable(page));
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
    public void IsValidPage_ShouldReturnCorrectValues(string? publishDate,
        string? date, bool? draft, bool draftOption, bool expectedValue)
    {
        var page = new Page(new(SourcePathConst, new FrontMatter
        {
            Title = TitleConst,
            PublishDate = publishDate is null
                ? null
                : DateTime.Parse(publishDate, CultureInfo.InvariantCulture),
            Date = date is null
                ? null
                : DateTime.Parse(date, CultureInfo.InvariantCulture),
            Draft = draft
        }), Site, "html", []);

        var options = Substitute.For<IGenerateOptions>();
        _ = options.Draft.Returns(draftOption);

        // Assert
        Assert.Equal(expectedValue, Site.IsPageValid(page, options));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void IsValidDate_ShouldReturnExpectedResult(bool futureOption,
        bool expected)
    {
        var page = new Page(new(SourcePathConst, new FrontMatter
        {
            Title = TitleConst,
            Date = SystemClockMock.Now.AddDays(1)
        }), Site, "html", []);

        // Act
        var options = Substitute.For<IGenerateOptions>();
        _ = options.Future.Returns(futureOption);

        // Assert
        Assert.Equal(expected, Site.IsDateValid(page, options));
    }

    [Theory]
    [InlineData("/test/path/index.md", "/test-title/index.html")]
    [InlineData("/another/path/index.md", "/test-title/index.html")]
    public void CreatePermalink_ShouldReturnCorrectUrl_WhenUrlIsNull(
        string sourcePath, string expectedUrl)
    {
        var page = new Page(new(sourcePath, new FrontMatter
        {
            Title = TitleConst,
        }), Site, "html", []);

        // Assert
        Assert.Equal(expectedUrl, page.CreatePermalink());
    }

    [Theory]
    [InlineData(null, "/test-title/index.html")]
    [InlineData("{{ page.Title }}/{{ page.SourceFileNameWithoutExtension }}",
        "/test-title/file/index.html")]
    public void Permalink_CreateWithDefaultOrCustomURLTemplate(
        string? urlTemplate, string expectedPermalink)
    {
        var page = new Page(new(SourcePathConst, new FrontMatter
        {
            Title = TitleConst,
            Url = urlTemplate
        }), Site, "html", []);
        var actualPermalink = page.CreatePermalink();

        // Assert
        Assert.Equal(expectedPermalink, actualPermalink);
    }

    [Theory]
    [InlineData(Kind.single, true)]
    [InlineData(Kind.list, false)]
    public void RegularPages_ShouldReturnCorrectPages_WhenKindIsSingle(
        Kind kind, bool isExpectedPage)
    {
        var page = new Page(new(SourcePathConst)
        {
            FrontMatter = new()
            {
                Title = TitleConst,
            },
            Kind = kind
        }
            , Site, "html", []);

        // Act
        Site.PostProcessPage(page);

        // Assert
        Assert.Equal(isExpectedPage, Site.RegularPages.Contains(page));
    }

    [Theory]
    [InlineData(Markdown1Const, 13)]
    [InlineData(Markdown2Const, 8)]
    public void WordCount_ShouldReturnCorrectCounts(string rawContent,
        int wordCountExpected)
    {
        var page = new Page(new(SourcePathConst, new(), rawContent), Site,
            "html", []);

        // Assert
        Assert.Equal(wordCountExpected, page.WordCount);
    }

    [Theory]
    [InlineData(Markdown1Const, MarkdownPlain1Const)]
    [InlineData(Markdown2Const, MarkdownPlain2Const)]
    public void Plain_ShouldReturnCorrectPlainString(string rawContent,
        string plain)
    {
        ArgumentException.ThrowIfNullOrEmpty(plain);
        var page = new Page(new(SourcePathConst, new(), rawContent), Site,
            "html", []);
        // Required to make the test pass on Windows
        plain = plain.Replace("\r\n", "\n", StringComparison.Ordinal);

        // Assert
        Assert.Equal(plain, page.Plain);
    }

    [Theory]
    // [InlineData("/pages/page-01", 3)]
    // [InlineData("/pages/page-01/page-01", 3)]
    // [InlineData("/blog/blog-01", 1)]
    // [InlineData("/blog/blog-01/blog-01", 1)]
    // [InlineData("/index/post-01", 1)]
    [InlineData("/index/post-01/post-01/index.html", 2)]
    // [InlineData("/articles/article-01", 0)]
    public void Cascade_ShouldCascadeValues(string url, int weight)
    {
        GenerateOptions options = new()
        {
            SourceArgument =
                Path.GetFullPath(Path.Combine(TestSitesPath,
                    TestSitePathConst09))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(new FileSystem());
        Site.ProcessPages();
        Site.OutputReferences.TryGetValue(url, out var itemPage);
        var page = itemPage as Page;

        // Assert
        Assert.Equal(weight, page!.Weight);
    }

    [Theory]
    [InlineData("/index/post-01/index.html", "cascade")]
    [InlineData("/index/post-01/post-01/index.html", "own")]
    public void Cascade_ShouldCascadeParams(string url, string? valueString)
    {
        GenerateOptions options = new()
        {
            SourceArgument =
                Path.GetFullPath(Path.Combine(TestSitesPath,
                    TestSitePathConst09))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(new FileSystem());
        Site.ProcessPages();
        Site.OutputReferences.TryGetValue(url, out var itemPage);
        var page = itemPage as Page;

        // Assert
        Assert.Equal(valueString, page!.Params["valueString"]);
    }

    [Fact]
    public void TemplateLookup_ShouldDefaultGenerateAllCombinations()
    {
        var page = new Page(new(string.Empty), Site, "html", []);

        // Act
        var paths = page.GetTemplateLookupOrder(false);

        // Assert
        Assert.Equal(6, paths.Count());
    }

    [Theory]
    [InlineData("", "", Kind.single, 4)]
    [InlineData("page", "", Kind.single, 6)]
    [InlineData("", "blog", Kind.single, 8)]
    [InlineData("page", "blog", Kind.single, 12)]
    [InlineData("post", "", Kind.home, 30)]
    [InlineData("post", "blog", Kind.section, 36)]
    public void TemplateLookup_ShouldGenerateAllCombinations(string type, string section, Kind kind, int expectedCount)
    {
        FrontMatter frontMatter = new()
        {
            Type = type,
            Section = section
        };
        ContentSource content = new(string.Empty)
        {
            FrontMatter = frontMatter,
            Kind = kind
        };
        var page = new Page(content, Site, "html", []);

        // Act
        var paths = page.GetTemplateLookupOrder(false).ToList();

        // Assert
        Assert.Equal(expectedCount, paths.Count());
        Assert.Contains($"_default/{kind}.liquid", paths);
        Assert.Contains($"_default/{kind}.html.liquid", paths);
        if (!string.IsNullOrEmpty(type))
        {
            Assert.Contains($"{type}/{kind}.liquid", paths);
            Assert.Contains($"{type}/{kind}.html.liquid", paths);
        }
    }
}
