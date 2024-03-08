using SuCoS.Models;
using SuCoS.Models.CommandLineOptions;
using Xunit;

namespace Tests.Models;

/// <summary>
/// Unit tests for the Site class.
/// </summary>
public class SiteTests : TestSetup
{
    [Theory]
    [InlineData("test01.md")]
    [InlineData("date-ok.md")]
    public void ScanAllMarkdownFiles_ShouldCountainFilenames(string fileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var siteFullPath = Path.GetFullPath(Path.Combine(testSitesPath, testSitePathCONST01));
        site.Options = new GenerateOptions
        {
            Source = siteFullPath
        };

        // Act
        site.ParseAndScanSourceFiles(Path.Combine(siteFullPath, "content"));

        // Assert
        Assert.Contains(site.Pages, page => page.SourceRelativePathDirectory!.Length == 0);
        Assert.Contains(site.Pages, page => page.SourceFileNameWithoutExtension == fileNameWithoutExtension);
    }

    [Theory]
    [InlineData(testSitePathCONST01)]
    [InlineData(testSitePathCONST02)]
    public void Home_ShouldReturnAHomePage(string sitePath)
    {
        GenerateOptions options = new()
        {
            Source = Path.GetFullPath(Path.Combine(testSitesPath, sitePath))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(site.SourceContentPath);

        // Assert
        Assert.NotNull(site.Home);
        Assert.True(site.Home.IsHome);
		_ = Assert.Single(site.OutputReferences.Values.Where(output => output is IPage page && page.IsHome));
    }

    [Theory]
    [InlineData(testSitePathCONST01, 0)]
    [InlineData(testSitePathCONST02, 0)]
    [InlineData(testSitePathCONST03, 1)]
    public void Page_IsSection_ShouldReturnExpectedQuantityOfPages(string sitePath, int expectedQuantity)
    {
        GenerateOptions options = new()
        {
            Source = Path.GetFullPath(Path.Combine(testSitesPath, sitePath))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

        // Assert
        Assert.Equal(expectedQuantity, site.OutputReferences.Values.Where(output => output is IPage page && page.IsSection).Count());
    }

    [Theory]
    [InlineData(testSitePathCONST01, 5)]
    [InlineData(testSitePathCONST02, 1)]
    [InlineData(testSitePathCONST03, 13)]
    [InlineData(testSitePathCONST04, 26)]
    public void PagesReference_ShouldReturnExpectedQuantityOfPages(string sitePath, int expectedQuantity)
    {
        GenerateOptions options = new()
        {
            Source = Path.GetFullPath(Path.Combine(testSitesPath, sitePath))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

        // Assert
        Assert.Equal(expectedQuantity, site.OutputReferences.Values.Where(output => output is IPage page).Count());
    }

    [Theory]
    [InlineData(testSitePathCONST01, 4)]
    [InlineData(testSitePathCONST02, 0)]
    [InlineData(testSitePathCONST03, 11)]
    [InlineData(testSitePathCONST04, 21)]
    public void Page_IsPage_ShouldReturnExpectedQuantityOfPages(string sitePath, int expectedQuantity)
    {
        GenerateOptions options = new()
        {
            Source = Path.GetFullPath(Path.Combine(testSitesPath, sitePath))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

        // Assert
        Assert.Equal(expectedQuantity, site.OutputReferences.Values.Where(output => output is IPage page && page.IsPage).Count());
    }

    [Fact]
    public void Page_Weight_ShouldReturnTheRightOrder()
    {
        GenerateOptions options = new()
        {
            Source = Path.GetFullPath(Path.Combine(testSitesPath, testSitePathCONST03))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

        // Assert
        Assert.Equal(100, site.RegularPages.First().Weight);
        Assert.Equal(-100, site.RegularPages.Last().Weight);
    }

    [Fact]
    public void Page_Weight_ShouldReturnZeroWeight()
    {
        GenerateOptions options = new()
        {
            Source = Path.GetFullPath(Path.Combine(testSitesPath, testSitePathCONST01))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

        // Assert
        Assert.Equal(0, site.RegularPages.First().Weight);
        Assert.Equal(0, site.RegularPages.Last().Weight);
    }

    [Fact]
    public void TagSectionPage_Pages_ShouldReturnNumberTagPages()
    {
        GenerateOptions options = new()
        {
            Source = Path.GetFullPath(Path.Combine(testSitesPath, testSitePathCONST04))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

		// Assert
		_ = site.OutputReferences.TryGetValue("/tags", out var output);
        var tagSectionPage = output as IPage;
        Assert.NotNull(tagSectionPage);
        Assert.Equal(2, tagSectionPage.Pages.Count());
        Assert.Empty(tagSectionPage.RegularPages);
        Assert.Equal("tags/index.md", tagSectionPage.SourceRelativePath);
        Assert.Equal("tags", tagSectionPage.SourceRelativePathDirectory);
        Assert.Equal("tags", tagSectionPage.SourcePathLastDirectory);
    }

    [Fact]
    public void TagPage_Pages_ShouldReturnNumberReferences()
    {
        GenerateOptions options = new()
        {
            Source = Path.GetFullPath(Path.Combine(testSitesPath, testSitePathCONST04))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

		// Assert
		_ = site.OutputReferences.TryGetValue("/tags/tag1", out var output);
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
            Source = Path.GetFullPath(Path.Combine(testSitesPath, testSitePathCONST04))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

		// Assert
		_ = site.OutputReferences.TryGetValue(url, out var output);
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
            Source = Path.GetFullPath(Path.Combine(testSitesPath, testSitePathCONST05))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

		// Assert
		_ = site.OutputReferences.TryGetValue(url, out var output);
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
            Source = Path.GetFullPath(Path.Combine(testSitesPath, testSitePathCONST07))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

		// Assert
		_ = site.OutputReferences.TryGetValue(url, out var output);
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
    public void Page_Content_ShouldReturnThemeContent(string url, string expectedContentPreRendered, string expectedContent, string expectedOutputfile)
    {
        GenerateOptions options = new()
        {
            Source = Path.GetFullPath(Path.Combine(testSitesPath, testSitePathCONST06))
        };
        site.Options = options;

        // Act
        site.ParseAndScanSourceFiles(null);

		// Assert
		_ = site.OutputReferences.TryGetValue(url, out var output);
        var page = output as IPage;
        Assert.NotNull(page);
        Assert.Equal(expectedContentPreRendered, page.ContentPreRendered);
        Assert.Equal(expectedContent, page.Content);
        Assert.Equal(expectedOutputfile, page.CompleteContent);
    }
}
