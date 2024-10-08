using SuCoS.Helpers;
using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using SuCoS.Parsers;
using Xunit;

namespace test.Models;

/// <summary>
/// Unit tests for the Site class.
/// </summary>
public class SiteTests : TestSetup
{
    private readonly IFileSystem _fs = new FileSystem();

    [Theory]
    [InlineData("test01.md")]
    [InlineData("date-ok.md")]
    public void ScanAllMarkdownFiles_ShouldContainFilenames(string fileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var siteFullPath = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst01));
        Site.Options = new GenerateOptions
        {
            SourceArgument = siteFullPath
        };

        // Act
        Site.ScanAndParseSourceFiles(_fs, Path.Combine(siteFullPath, "content"));
        Site.ProcessPages();

        // Assert
        Assert.Contains(Site.Pages, page => page.SourceRelativePathDirectory!.Length == 0);
        Assert.Contains(Site.Pages, page => page.SourceFileNameWithoutExtension == fileNameWithoutExtension);
    }

    [Theory]
    [InlineData(TestSitePathConst01)]
    [InlineData(TestSitePathConst02)]
    public void Home_ShouldReturnAHomePage(string sitePath)
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, sitePath))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(_fs, Site.SourceContentPath);
        Site.ProcessPages();

        // Assert
        Assert.NotNull(Site.Home);
        Assert.True(Site.Home.IsHome);
        Assert.Single(Site.OutputReferences.Values, output => output is IPage
        {
            IsHome: true
        });
    }

    [Theory]
    [InlineData(TestSitePathConst01, 0)]
    [InlineData(TestSitePathConst02, 0)]
    [InlineData(TestSitePathConst03, 1)]
    public void Page_IsSection_ShouldReturnExpectedQuantityOfPages(string sitePath, int expectedQuantity)
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, sitePath))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        Assert.Equal(expectedQuantity, Site.OutputReferences.Values.Count(output => output is IPage
        {
            Kind: Kind.section
        }));
    }

    [Theory]
    [InlineData(TestSitePathConst01, 5)]
    [InlineData(TestSitePathConst02, 8)]
    [InlineData(TestSitePathConst03, 13)]
    [InlineData(TestSitePathConst04, 26)]
    public void PagesReference_ShouldReturnExpectedQuantityOfPages(string sitePath, int expectedQuantity)
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, sitePath))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        Assert.Equal(expectedQuantity, Site.OutputReferences.Values.Count(output => output is IPage));
    }

    [Theory]
    [InlineData(TestSitePathConst01, 4)]
    [InlineData(TestSitePathConst02, 7)]
    [InlineData(TestSitePathConst03, 11)]
    [InlineData(TestSitePathConst04, 21)]
    public void Page_IsPage_ShouldReturnExpectedQuantityOfPages(string sitePath, int expectedQuantity)
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, sitePath))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        Assert.Equal(expectedQuantity, Site.OutputReferences.Values.Count(output => output is IPage
        {
            IsPage: true
        }));
    }

    [Fact]
    public void Page_Weight_ShouldReturnTheRightOrder()
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst03))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        Assert.Equal(100, Site.RegularPages.First().Weight);
        Assert.Equal(-100, Site.RegularPages.Last().Weight);
    }

    [Fact]
    public void Page_Weight_ShouldReturnZeroWeight()
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst01))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        Assert.Equal(0, Site.RegularPages.First().Weight);
        Assert.Equal(0, Site.RegularPages.Last().Weight);
    }

    [Fact]
    public void TagSectionPage_Pages_ShouldReturnNumberTagPages()
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst04))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        Site.OutputReferences.TryGetValue("/tags", out var output);
        var tagSectionPage = output as IPage;
        Assert.NotNull(tagSectionPage);
        Assert.Equal(10, tagSectionPage.Pages.Count());
        Assert.Equal(10, tagSectionPage.RegularPages.Count());
        Assert.Equal("tags/_index.md", tagSectionPage.SourceRelativePath);
        Assert.Equal("tags", tagSectionPage.SourceRelativePathDirectory);
        Assert.Equal("tags", tagSectionPage.SourcePathLastDirectory);
    }

    [Fact]
    public void TagPage_Pages_ShouldReturnNumberReferences()
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst04))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        _ = Site.OutputReferences.TryGetValue("/tags/tag1", out var output);
        var page = output as IPage;
        Assert.NotNull(page);
        Assert.Equal(10, page.Pages.Count());
        Assert.Equal(10, page.RegularPages.Count());
    }

    [Theory]
    [InlineData("/", "<p>Index Content</p>\n")]
    [InlineData("/blog", "")]
    [InlineData("/tags", "")]
    [InlineData("/tags/tag1", "")]
    [InlineData("/blog/test-content-1", "<p>Test Content 1</p>\n")]
    public void Page_Content_ShouldReturnNullThemeContent(string url, string expectedContent)
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst04))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        _ = Site.OutputReferences.TryGetValue(url, out var output);
        var page = output as IPage;
        Assert.NotNull(page);
        Assert.Equal(expectedContent, page.Content);
        Assert.Equal(page.ContentPreRendered, page.Content);
    }

    [Theory]
    [InlineData("/",
    "<p>Index Content</p>\n",
    "INDEX-<p>Index Content</p>\n")]
    [InlineData("/blog",
    "",
    "LIST-")]
    [InlineData("/tags",
    "",
    "LIST-")]
    [InlineData("/tags/tag1",
    "",
    "LIST-")]
    [InlineData("/blog/test-content-1",
    "<p>Test Content 1</p>\n",
    "SINGLE-<p>Test Content 1</p>\n")]
    public void Page_Content_ShouldReturnNullThemeBaseofContent(string url, string expectedContentPreRendered, string expectedContent)
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst05))
        };
        var parser = new YamlParser();
        var siteSettings = SiteHelper.ParseSettings("sucos.yaml", options, parser, _fs);
        Site = new Site(options, siteSettings, parser, LoggerMock, null);

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        _ = Site.OutputReferences.TryGetValue(url, out var output);
        var page = output as IPage;
        Assert.NotNull(page);
        Assert.Equal(expectedContentPreRendered, page.ContentPreRendered);
        Assert.Equal(expectedContent, page.Content);
        Assert.Equal(expectedContent, page.CompleteContent);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/blog")]
    [InlineData("/tags")]
    [InlineData("/tags/tag1")]
    [InlineData("/blog/test-content-1")]
    public void Page_Content_ShouldReturnThrowNullThemeBaseofContent(string url)
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst07))
        };
        var parser = new YamlParser();
        var siteSettings = SiteHelper.ParseSettings("sucos.yaml", options, parser, _fs);
        Site = new Site(options, siteSettings, parser, LoggerMock, null);

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        _ = Site.OutputReferences.TryGetValue(url, out var output);
        var page = output as IPage;
        Assert.NotNull(page);
        Assert.Equal(string.Empty, page.Content);
        Assert.Equal(string.Empty, page.CompleteContent);
    }

    [Theory]
    [InlineData("/",
    "<p>Index Content</p>\n",
    "INDEX-<p>Index Content</p>\n",
    "BASEOF-INDEX-<p>Index Content</p>\n")]
    [InlineData("/blog",
    "",
    "LIST-",
    "BASEOF-LIST-")]
    [InlineData("/tags",
    "",
    "LIST-",
    "BASEOF-LIST-")]
    [InlineData("/tags/tag1",
    "",
    "LIST-",
    "BASEOF-LIST-")]
    [InlineData("/blog/test-content-1",
    "<p>Test Content 1</p>\n",
    "SINGLE-<p>Test Content 1</p>\n",
    "BASEOF-SINGLE-<p>Test Content 1</p>\n")]
    public void Page_Content_ShouldReturnThemeContent(string url, string expectedContentPreRendered, string expectedContent, string expectedOutputFile)
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst06))
        };
        var parser = new YamlParser();
        var siteSettings = SiteHelper.ParseSettings("sucos.yaml", options, parser, _fs);
        Site = new Site(options, siteSettings, parser, LoggerMock, null);

        // Act
        Site.ScanAndParseSourceFiles(_fs);
        Site.ProcessPages();

        // Assert
        _ = Site.OutputReferences.TryGetValue(url, out var output);
        var page = output as IPage;
        Assert.NotNull(page);
        Assert.Equal(expectedContentPreRendered, page.ContentPreRendered);
        Assert.Equal(expectedContent, page.Content);
        Assert.Equal(expectedOutputFile, page.CompleteContent);
    }

    [Fact]
    public void Site_ShouldConsiderSectionPages()
    {
        GenerateOptions options = new()
        {
            SourceArgument = Path.GetFullPath(Path.Combine(TestSitesPath, TestSitePathConst09))
        };
        Site.Options = options;

        // Act
        Site.ScanAndParseSourceFiles(new FileSystem(), null);
        Site.ProcessPages();

        // Assert
        Assert.Equal(12, Site.OutputReferences.Values.Count(output => output is IPage));
        // Assert.Equal(20, Site.OutputReferences.Count);
        Assert.True(Site.OutputReferences.ContainsKey("/pages/page-01"));
        Assert.True(Site.OutputReferences.ContainsKey("/blog/blog-01"));
        Assert.True(Site.OutputReferences.ContainsKey("/pages/page-01/page-01"));
        Assert.True(Site.OutputReferences.ContainsKey("/blog/blog-01/blog-01"));
        Assert.True(Site.OutputReferences.ContainsKey("/articles/article-01"));
        Assert.True(Site.OutputReferences.ContainsKey("/index/post-01"));
        Assert.True(Site.OutputReferences.ContainsKey("/index/post-01/post-01"));
    }
}
