using SuCoS.Helpers;
using SuCoS.Models;
using System.Globalization;
using SuCoS.Parsers;
using Xunit;

namespace Tests.YAMLParser;

public class YamlParserTests : TestSetup
{
    private readonly YamlParser _parser = new();

    private const string PageFrontMatterConst = """
                                                Title: Test Title
                                                Type: post
                                                Date: 2023-07-01
                                                LastMod: 2023-06-01
                                                PublishDate: 2023-06-01
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
        var frontMatter = FrontMatter.Parse(FileRelativePathConst, FileFullPathConst, _parser, fileContent);

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
        var frontMatter = FrontMatter.Parse(FileRelativePathConst, FileFullPathConst, _parser, fileContent);

        // Assert
        Assert.Equal(expectedDate, frontMatter.Date);
    }

    [Fact]
    public void ParseFrontMatter_ShouldParseOtherFieldsCorrectly()
    {
        // Arrange
        var expectedDate = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
        var expectedLastMod = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedPublishDate = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedExpiryDate = DateTime.Parse("2024-06-01", CultureInfo.InvariantCulture);

        // Act
        var frontMatter = FrontMatter.Parse(FileRelativePathConst, FileFullPathConst, _parser, PageContent);

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
        Assert.Equal("https://www.example.com/", siteSettings.BaseURL);
        Assert.Equal("Tastiest C# Static Site Generator of the World", siteSettings.Description);
        Assert.Equal("Copyright message", siteSettings.Copyright);
    }

    [Fact]
    public void ParseParams_ShouldFillParamsWithNonMatchingFields()
    {
        // Arrange
        var frontMatter = FrontMatter.Parse(string.Empty, string.Empty, _parser, PageContent);
        var page = new Page(frontMatter, Site);

        // Assert
        Assert.False(page.Params.ContainsKey("customParam"));
        Assert.Equal("Custom Value", page.Params["ParamsCustomParam"]);
    }

    [Fact]
    public void ParseFrontMatter_ShouldParseContentInSiteFolder()
    {
        // Arrange
        var date = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
        var frontMatter = FrontMatter.Parse(string.Empty, string.Empty, _parser, PageContent);
        Page page = new(frontMatter, Site);

        // Act
        Site.PostProcessPage(page);

        // Assert
        Assert.Equal(date, frontMatter.Date);
    }

    [Fact]
    public void ParseFrontMatter_ShouldCreateTags()
    {
        // Arrange
        var frontMatter = FrontMatter.Parse(string.Empty, string.Empty, _parser, PageContent);
        Page page = new(frontMatter, Site);

        // Act
        Site.PostProcessPage(page);

        // Assert
        Assert.Equal(2, page.TagsReference.Count);
    }

    [Fact]
    public void FrontMatterParse_RawContentNull()
    {
        _ = Assert.Throws<FormatException>(() => FrontMatter.Parse("invalidFrontMatter", "", "fakePath", "fakePath", FrontMatterParser));
    }

    [Fact]
    public void ParseYAML_ShouldThrowExceptionWhenFrontMatterIsInvalid()
    {
        _ = Assert.Throws<FormatException>(() => _parser.Parse<FrontMatter>("invalidFrontMatter"));
    }

    [Fact]
    public void ParseYAML_ShouldSplitTheMetadata()
    {
        // Act
        var (metadata, rawContent) = _parser.SplitFrontMatter(PageContent);

        // Assert
        Assert.Equal(PageFrontMatterConst.TrimEnd(), metadata);
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
        Assert.Equal("https://www.example.com/", siteSettings.BaseURL);
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
}
