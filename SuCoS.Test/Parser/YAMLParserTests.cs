using System.Globalization;
using SuCoS.Helpers;
using SuCoS.Models;
using SuCoS.Parsers;
using Xunit;

namespace test.Parser;

public class YamlParserTests : TestSetup
{
    private readonly YamlParser _parser = new();

    private const string PageFrontMatterConst = """
                                                Title: Test Title
                                                Type: post
                                                Date: 2023-01-01
                                                LastMod: 2023-02-01
                                                PublishDate: 2023-03-01
                                                ExpiryDate: 2024-06-01
                                                Tags:
                                                  - Test
                                                  - Real Data
                                                Categories:
                                                  - Test
                                                  - Real Data
                                                NestedData:
                                                  Level2:
                                                    - Test
                                                    - Real Data
                                                customParam: Custom Value
                                                Params:
                                                  ParamsCustomParam: Custom Value
                                                  ParamsNestedData:
                                                    Level2:
                                                      - Test
                                                      - Real Data

                                                """;
    private const string PageMarkdownConst = """
                                             # Real Data Test

                                             This is a test using real data. Real Data Test

                                             """;
    private const string SiteContentConst = """
                                            Title: My Site
                                            BaseURL: https://www.example.com/
                                            Description: Tastiest C# Static Site Generator of the World
                                            Copyright: Copyright message
                                            customParam: Custom Value
                                            NestedData:
                                              Level2:
                                                - Test
                                                - Real Data
                                            Params:
                                              ParamsCustomParam: Custom Value
                                              ParamsNestedData:
                                                Level2:
                                                  - Test
                                                  - Real Data

                                            """;
    private const string FileFullPathConst = "test.md";
    private const string FileRelativePathConst = "test.md";
    private const string PageContent = $"""
                                         ---
                                         {PageFrontMatterConst}
                                         ---
                                         {PageMarkdownConst}
                                         """;

    private static readonly string[] Expected = ["Test", "Real Data"];

    [Fact]
    public void GetSection_ShouldReturnFirstFolderName()
    {
        // Arrange
        var filePath = Path.Combine("folder1", "folder2", "file.md");

        // Act
        var section = SiteHelper.GetSection(filePath);

        // Assert
        Assert.Equal("folder1", section);
    }

    [Theory]
    [InlineData("""
                ---
                Title: Test Title
                ---

                """, "Test Title")]
    [InlineData("""
                ---
                Date: 2023-04-01
                ---

                """, "")]
    public void ParseFrontMatter_ShouldParseTitleCorrectly(string fileContent, string expectedTitle)
    {
        // Arrange
        (var frontMatter, _) = FrontMatter.Parse(FileRelativePathConst, FileFullPathConst, _parser, fileContent);

        // Assert
        Assert.Equal(expectedTitle, frontMatter.Title);
    }

    [Theory]
    [InlineData("""
                ---
                Date: 2023-01-01
                ---

                """, "2023-01-01")]
    [InlineData("""
                ---
                Date: 2023/01/01
                ---

                """, "2023-01-01")]
    public void ParseFrontMatter_ShouldParseDateCorrectly(string fileContent, string expectedDateString)
    {
        // Arrange
        var expectedDate = DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture);

        // Act
        (var frontMatter, _) = FrontMatter.Parse(FileRelativePathConst, FileFullPathConst, _parser, fileContent);

        // Assert
        Assert.Equal(expectedDate, frontMatter.Date);
    }

    [Fact]
    public void ParseFrontMatter_ShouldParseOtherFieldsCorrectly()
    {
        // Arrange
        var expectedDate = DateTime.Parse("2023-01-01", CultureInfo.InvariantCulture);
        var expectedLastMod = DateTime.Parse("2023-02-01", CultureInfo.InvariantCulture);
        var expectedPublishDate = DateTime.Parse("2023-03-01", CultureInfo.InvariantCulture);
        var expectedExpiryDate = DateTime.Parse("2024-06-01", CultureInfo.InvariantCulture);

        // Act
        (var frontMatter, _) = FrontMatter.Parse(FileRelativePathConst, FileFullPathConst, _parser, PageContent);

        // Assert
        Assert.Equal("Test Title", frontMatter.Title);
        Assert.Equal("post", frontMatter.Type);
        Assert.Equal(expectedDate, frontMatter.Date);
        Assert.Equal(expectedLastMod, frontMatter.LastMod);
        Assert.Equal(expectedPublishDate, frontMatter.PublishDate);
        Assert.Equal(expectedExpiryDate, frontMatter.ExpiryDate);
    }

    [Fact]
    public void ParseFrontMatter_ShouldThrowFormatException_WhenInvalidYAMLSyntax()
    {
        // Arrange
        const string fileContent = """
                                   ---
                                   Title
                                   ---

                                   """;

        // Assert
        Assert.Throws<FormatException>(() =>
            FrontMatter.Parse(FileRelativePathConst, FileFullPathConst, _parser, fileContent));
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnSiteWithCorrectSettings()
    {
        // Act
        var siteSettings = _parser.Parse<SiteSettings>(SiteContentConst);


        // Assert
        Assert.Equal("My Site", siteSettings.Title);
        Assert.Equal("https://www.example.com/", siteSettings.BaseUrl);
        Assert.Equal("Tastiest C# Static Site Generator of the World", siteSettings.Description);
        Assert.Equal("Copyright message", siteSettings.Copyright);
    }

    [Fact]
    public void ParseParams_ShouldFillParamsWithNonMatchingFields()
    {
        // Arrange
        (var frontMatter, _) = FrontMatter.Parse(string.Empty, string.Empty, _parser, PageContent);
        ContentSource contentSource = new(string.Empty, frontMatter);
        var page = new Page(contentSource, Site);

        // Assert
        Assert.False(page.Params.ContainsKey("customParam"));
        Assert.Equal("Custom Value", page.Params["ParamsCustomParam"]);
    }

    [Fact]
    public void ParseFrontMatter_ShouldParseContentInSiteFolder()
    {
        // Arrange
        var date = DateTime.Parse("2023-01-01", CultureInfo.InvariantCulture);
        (var frontMatter, _) = FrontMatter.Parse(string.Empty, string.Empty, _parser, PageContent);
        ContentSource contentSource = new(string.Empty, frontMatter);
        var page = new Page(contentSource, Site);

        // Act
        Site.PostProcessPage(page);

        // Assert
        Assert.Equal(date, frontMatter.Date);
    }

    // [Fact(Skip = "Not done in the  ")]
    [Fact]
    public void ParseFrontMatter_ShouldCreateTags()
    {
        // Arrange
        (var frontMatter, _) = FrontMatter.Parse(string.Empty, string.Empty, _parser, PageContent);
        ContentSource contentSource = new(string.Empty, frontMatter);

        // Act
        Site.ContentSourceAdd(contentSource);
        Site.ProcessPages();
        Site.OutputReferences.TryGetValue("/test-title", out var output);
        var page = output as Page;

        // Assert
        Assert.Equal(2, page?.TagsReference.Count);
    }

    [Fact]
    public void FrontMatterParse_RawContentNull()
    {
        _ = Assert.Throws<FormatException>(() => FrontMatter.Parse("invalidFrontMatter", "fakePath", "fakePath", FrontMatterParser));
    }

    [Fact]
    public void ParseYAML_ShouldThrowExceptionWhenFrontMatterIsInvalid()
    {
        _ = Assert.Throws<FormatException>(() => _parser.Parse<FrontMatter>("invalidFrontMatter"));
    }

    [Fact]
    public void ParseYAML_ShouldSplitTheFrontMatter()
    {
        // Act
        var (frontMatter, rawContent) = _parser.SplitFrontMatterAndContent(PageContent);
        frontMatter = frontMatter.Replace("\r\n", "\n", StringComparison.Ordinal);

        // Assert
        Assert.Equal(PageFrontMatterConst.TrimEnd(), frontMatter);
        Assert.Equal(PageMarkdownConst, rawContent);
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnSiteSettings()
    {
        // Arrange
        var siteSettings = _parser.Parse<SiteSettings>(SiteContentConst);

        // Assert
        Assert.NotNull(siteSettings);
        Assert.Equal("My Site", siteSettings.Title);
        Assert.Equal("https://www.example.com/", siteSettings.BaseUrl);
    }


    [Fact]
    public void SiteParams_ShouldHandleEmptyContent()
    {
        Assert.Empty(Site.Params);
    }

    [Fact]
    public void SiteParams_ShouldPopulateParamsWithExtraFields()
    {
        // Arrange
        var siteSettings = _parser.Parse<SiteSettings>(SiteContentConst);
        Site = new Site(GenerateOptionsMock, siteSettings, FrontMatterParser, LoggerMock, SystemClockMock);

        // Assert
        Assert.NotEmpty(siteSettings.Params);
        Assert.DoesNotContain("customParam", Site.Params);
        Assert.Contains("ParamsCustomParam", Site.Params);
        Assert.Equal("Custom Value", Site.Params["ParamsCustomParam"]);
        Assert.Equal(Expected, ((Dictionary<string, object>)siteSettings.Params["ParamsNestedData"])["Level2"]);
        Assert.Equal("Test", ((siteSettings.Params["ParamsNestedData"] as Dictionary<string, object>)?["Level2"] as List<object>)?[0]);
    }

    [Theory]
    [InlineData("""
                ---
                Title: title-test
                Url: my-page
                ---
                """)]
    [InlineData("""
                ---
                title: title-test
                url: my-page
                ---
                """)]
    [InlineData("""
                ---
                tiTle: title-test
                URL: my-page
                ---
                """)]
    [InlineData("""
                ---
                tiTle: title-test-old
                title: title-test       # the last on is used
                Url: my-page
                url: my-page
                ---
                """)]
    public void FrontMatter_ShouldIgnoreCase(string fileContent)
    {
        // Arrange
        (var frontMatter, _) = FrontMatter.Parse(FileRelativePathConst, FileFullPathConst, _parser, fileContent);

        // Assert
        Assert.Equal("title-test", frontMatter.Title);
        Assert.Equal("my-page", frontMatter.Url);
    }

    [Theory]
    [InlineData("""
                Title: title-test
                BaseURL: https://www.example.com/
                """)]
    [InlineData("""
                title: title-test
                baseurl: https://www.example.com/
                """)]
    [InlineData("""
                tiTle: title-test
                baseUrl: https://www.example.com/
                """)]
    [InlineData("""
                tiTle: title-test-old
                Title: title-test                   # the last on is used
                baseurl: https://www.example2.com/
                BaseURL: https://www.example.com/   # the last on is used
                """)]
    public void SiteSettings_ShouldIgnoreCase(string fileContent)
    {
        // Arrange
        var siteSettings = _parser.Parse<SiteSettings>(fileContent);
        Site = new Site(GenerateOptionsMock, siteSettings, FrontMatterParser, LoggerMock, SystemClockMock);

        // Assert
        Assert.Equal("title-test", Site.Title);
        Assert.Equal("https://www.example.com/", Site.BaseUrl);
    }
}
