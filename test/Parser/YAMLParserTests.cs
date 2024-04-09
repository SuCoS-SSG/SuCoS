using SuCoS.Helpers;
using SuCoS.Models;
using System.Globalization;
using Xunit;

namespace Tests.YAMLParser;

public class YAMLParserTests : TestSetup
{
  private readonly SuCoS.Parser.YAMLParser parser;

  private const string pageFrontmaterCONST = @"Title: Test Title
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
";
  private const string pageMarkdownCONST = @"
# Real Data Test

This is a test using real data. Real Data Test
";
  private const string siteContentCONST = @"
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
";
  private const string fileFullPathCONST = "test.md";
  private const string fileRelativePathCONST = "test.md";
  private readonly string pageContent;

  public YAMLParserTests()
  {
        parser = new SuCoS.Parser.YAMLParser();
    pageContent = @$"---
{pageFrontmaterCONST}
---
{pageMarkdownCONST}";
  }

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
  [InlineData(@"---
Title: Test Title
---
", "Test Title")]
  [InlineData(@"---
Date: 2023-04-01
---
", "")]
  public void ParseFrontmatter_ShouldParseTitleCorrectly(string fileContent, string expectedTitle)
  {
    // Arrange
    var frontMatter = parser.ParseFrontmatterAndMarkdown(fileRelativePathCONST, fileFullPathCONST, fileContent);

    // Assert
    Assert.Equal(expectedTitle, frontMatter.Title);
  }

  [Theory]
  [InlineData(@"---
Date: 2023-01-01
---
", "2023-01-01")]
  [InlineData(@"---
Date: 2023/01/01
---
", "2023-01-01")]
  public void ParseFrontmatter_ShouldParseDateCorrectly(string fileContent, string expectedDateString)
  {
    // Arrange
    var expectedDate = DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture);

    // Act
    var frontMatter = parser.ParseFrontmatterAndMarkdown(fileRelativePathCONST, fileFullPathCONST, fileContent);

    // Assert
    Assert.Equal(expectedDate, frontMatter.Date);
  }

  [Fact]
  public void ParseFrontmatter_ShouldParseOtherFieldsCorrectly()
  {
    // Arrange
    var expectedDate = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
    var expectedLastMod = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
    var expectedPublishDate = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
    var expectedExpiryDate = DateTime.Parse("2024-06-01", CultureInfo.InvariantCulture);

    // Act
    var frontMatter = parser.ParseFrontmatterAndMarkdown(fileRelativePathCONST, fileFullPathCONST, pageContent);

    // Assert
    Assert.Equal("Test Title", frontMatter.Title);
    Assert.Equal("post", frontMatter.Type);
    Assert.Equal(expectedDate, frontMatter.Date);
    Assert.Equal(expectedLastMod, frontMatter.LastMod);
    Assert.Equal(expectedPublishDate, frontMatter.PublishDate);
    Assert.Equal(expectedExpiryDate, frontMatter.ExpiryDate);
  }

  [Fact]
  public void ParseFrontmatter_ShouldThrowException_WhenInvalidYAMLSyntax()
  {
    // Arrange
    const string fileContent = @"---
Title
---
";

    // Assert
    Assert.Throws<InvalidCastException>(() => parser.ParseFrontmatterAndMarkdown(fileRelativePathCONST, fileFullPathCONST, fileContent));
  }

  [Fact]
  public void ParseSiteSettings_ShouldReturnSiteWithCorrectSettings()
  {
    // Act
    var siteSettings = parser.Parse<SiteSettings>(siteContentCONST);


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
    var page = new Page(parser.ParseFrontmatterAndMarkdown(string.Empty, string.Empty, pageContent), site);

      // Assert
      Assert.False(page.Params.ContainsKey("customParam"));
      Assert.Equal("Custom Value", page.Params["ParamsCustomParam"]);
  }

  [Fact]
  public void ParseFrontmatter_ShouldParseContentInSiteFolder()
  {
    // Arrange
    var date = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
    var frontMatter = parser.ParseFrontmatterAndMarkdown("", "", pageContent);
    Page page = new(frontMatter, site);

    // Act
    site.PostProcessPage(page);

    // Assert
    Assert.Equal(date, frontMatter.Date);
  }

  [Fact]
  public void ParseFrontmatter_ShouldCreateTags()
  {
    // Arrange
    var frontMatter = parser.ParseFrontmatterAndMarkdown("", "", pageContent);
    Page page = new(frontMatter, site);

    // Act
    site.PostProcessPage(page);

    // Assert
    Assert.Equal(2, page.TagsReference.Count);
  }

  [Fact]
  public void ParseFrontmatter_ShouldThrowExceptionWhenSiteIsNull()
  {
    _ = Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdownFromFile(null!, "fakeFilePath"));
  }

  [Fact]
  public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathIsNull()
  {
    _ = Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdownFromFile(null!));
  }

  [Fact]
  public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathDoesNotExist()
  {
    _ = Assert.Throws<FileNotFoundException>(() => parser.ParseFrontmatterAndMarkdownFromFile("fakePath"));
  }

  [Fact]
  public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathDoesNotExist2()
  {
    _ = Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdown(null!, null!, "fakeContent"));
  }

  [Fact]
  public void ParseFrontmatter_ShouldHandleEmptyFileContent()
  {
    _ = Assert.Throws<FormatException>(() => parser.ParseFrontmatterAndMarkdown("fakeFilePath", "/fakeFilePath", ""));
  }

  [Fact]
  public void ParseYAML_ShouldThrowExceptionWhenFrontmatterIsInvalid()
  {
    _ = Assert.Throws<FormatException>(() => parser.ParseFrontmatterAndMarkdown("fakeFilePath", "/fakeFilePath", "invalidFrontmatter"));
  }

  [Fact]
  public void ParseSiteSettings_ShouldReturnSiteSettings()
  {
    // Arrange
    var siteSettings = parser.Parse<SiteSettings>(siteContentCONST);

    // Assert
    Assert.NotNull(siteSettings);
    Assert.Equal("My Site", siteSettings.Title);
    Assert.Equal("https://www.example.com/", siteSettings.BaseURL);
  }

  [Fact]
  public void ParseSiteSettings_ShouldReturnContent()
  {
    // Arrange
    var frontMatter = parser.ParseFrontmatterAndMarkdown("fakeFilePath", "/fakeFilePath", pageContent);

    // Assert
    Assert.Equal(pageMarkdownCONST, frontMatter.RawContent);
  }


  [Fact]
  public void SiteParams_ShouldHandleEmptyContent()
  {
    Assert.Empty(site.Params);
  }

  [Fact]
  public void SiteParams_ShouldPopulateParamsWithExtraFields()
  {
    // Arrange
    var siteSettings = parser.Parse<SiteSettings>(siteContentCONST);
    site = new Site(generateOptionsMock, siteSettings, frontMatterParser, loggerMock, systemClockMock);

    // Assert
    Assert.NotEmpty(siteSettings.Params);
    Assert.DoesNotContain("customParam", site.Params);
    Assert.Contains("ParamsCustomParam", site.Params);
    Assert.Equal("Custom Value", site.Params["ParamsCustomParam"]);
    Assert.Equal(new[] { "Test", "Real Data" }, ((Dictionary<string, object>)siteSettings.Params["ParamsNestedData"])["Level2"]);
    Assert.Equal("Test", ((siteSettings.Params["ParamsNestedData"] as Dictionary<string, object>)?["Level2"] as List<object>)?[0]);
  }
}
