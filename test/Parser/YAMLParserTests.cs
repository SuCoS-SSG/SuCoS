using Xunit;
using Moq;
using SuCoS.Parser;
using System.Globalization;
using SuCoS.Helpers;
using SuCoS.Models;

namespace Test.Parser;

public class YAMLParserTests
{
    private readonly YAMLParser parser;
    private readonly Mock<Site> siteDefault;

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
";
    private const string pageMarkdownCONST = @"
# Real Data Test

This is a test using real data. Real Data Test
";
    private const string siteContentCONST = @"
Title: My Site
BaseUrl: https://www.example.com/
customParam: Custom Value
NestedData:
  Level2:
    - Test
    - Real Data
";
    private const string filePathCONST = "test.md";
    private readonly string pageContent;

    public YAMLParserTests()
    {
        parser = new YAMLParser();
        siteDefault = new Mock<Site>();
        pageContent = @$"---
{pageFrontmaterCONST}
---
{pageMarkdownCONST}";
    }

    [Fact]
    public void GetSection_ShouldReturnFirstFolderName()
    {
        var filePath = Path.Combine("folder1", "folder2", "file.md");

        // Act
        var section = SiteHelper.GetSection(filePath);

        // Asset
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
        // Act
        var frontmatter = parser.ParseFrontmatterAndMarkdown(siteDefault.Object, filePathCONST, fileContent);

        // Asset
        Assert.Equal(expectedTitle, frontmatter.Title);
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowException_WhenSiteIsNull()
    {
        // Asset
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdown(null!, filePathCONST, pageContent));
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
        var expectedDate = DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture);

        // Act
        var frontmatter = parser.ParseFrontmatterAndMarkdown(siteDefault.Object, filePathCONST, fileContent);

        // Asset
        Assert.Equal(expectedDate, frontmatter.Date);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseOtherFieldsCorrectly()
    {
        var expectedDate = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
        var expectedLastMod = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedPublishDate = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedExpiryDate = DateTime.Parse("2024-06-01", CultureInfo.InvariantCulture);

        // Act
        var frontmatter = parser.ParseFrontmatterAndMarkdown(siteDefault.Object, filePathCONST, pageContent);

        // Asset
        Assert.Equal("Test Title", frontmatter.Title);
        Assert.Equal("post", frontmatter.Type);
        Assert.Equal(expectedDate, frontmatter.Date);
        Assert.Equal(expectedLastMod, frontmatter.LastMod);
        Assert.Equal(expectedPublishDate, frontmatter.PublishDate);
        Assert.Equal(expectedExpiryDate, frontmatter.ExpiryDate);
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowException_WhenInvalidYAMLSyntax()
    {
        const string fileContent = @"---
Title
---
";

        // Asset
        Assert.Throws<YamlDotNet.Core.YamlException>(() => parser.ParseFrontmatterAndMarkdown(siteDefault.Object, filePathCONST, fileContent));
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnSiteWithCorrectSettings()
    {
        // Act
        var site = parser.ParseSiteSettings(siteContentCONST);

        // Asset
        Assert.Equal("https://www.example.com/", site.BaseUrl);
        Assert.Equal("My Site", site.Title);
    }

    [Fact]
    public void ParseParams_ShouldFillParamsWithNonMatchingFields()
    {
        var page = new Frontmatter("Test Title", "/test.md", siteDefault.Object);

        // Act
        parser.ParseParams(page, typeof(Frontmatter), pageFrontmaterCONST);

        // Asset
        Assert.True(page.Params.ContainsKey("customParam"));
        Assert.Equal("Custom Value", page.Params["customParam"]);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseContentInSiteFolder()
    {
        var date = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
        var frontmatter = parser.ParseFrontmatterAndMarkdown(siteDefault.Object, "", pageContent);

        // Act
        siteDefault.Object.PostProcessFrontMatter(frontmatter);

        // Asset
        Assert.Equal(date, frontmatter.Date);
    }

    [Fact]
    public void ParseFrontmatter_ShouldCreateTags()
    {
        var frontmatter = parser.ParseFrontmatterAndMarkdown(siteDefault.Object, "", pageContent);

        // Asset
        Assert.Equal(2, frontmatter.Tags?.Count);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseCategoriesCorrectly()
    {
        var frontmatter = parser.ParseFrontmatterAndMarkdown(siteDefault.Object, "fakeFilePath", pageContent);

        // Asset
        Assert.Equal(new[] { "Test", "Real Data" }, frontmatter.Params["Categories"]);
        Assert.Equal(new[] { "Test", "Real Data" }, (frontmatter.Params["NestedData"] as Dictionary<object, object>)?["Level2"]);
        Assert.Equal("Test", ((frontmatter.Params["NestedData"] as Dictionary<object, object>)?["Level2"] as List<object>)?[0]);
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenSiteIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdownFromFile(null!, "fakeFilePath"));
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdownFromFile(siteDefault.Object, null!));
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() => parser.ParseFrontmatterAndMarkdownFromFile(siteDefault.Object, "fakePath"));
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathDoesNotExist2()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdown(siteDefault.Object, null!, "fakeContent"));
    }

    [Fact]
    public void ParseFrontmatter_ShouldHandleEmptyFileContent()
    {
        Assert.Throws<FormatException>(() => parser.ParseFrontmatterAndMarkdown(siteDefault.Object, "fakeFilePath", ""));
    }

    [Fact]
    public void ParseYAML_ShouldThrowExceptionWhenFrontmatterIsInvalid()
    {
        Assert.Throws<FormatException>(() => parser.ParseFrontmatterAndMarkdown(siteDefault.Object, "fakeFilePath", "invalidFrontmatter"));
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnSiteSettings()
    {
        var site = parser.ParseSiteSettings(siteContentCONST);
        Assert.NotNull(site);
        Assert.Equal("My Site", site.Title);
        Assert.Equal("https://www.example.com/", site.BaseUrl);
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnContent()
    {
        var frontmatter = parser.ParseFrontmatterAndMarkdown(siteDefault.Object, "fakeFilePath", pageContent);

        Assert.Equal(pageMarkdownCONST, frontmatter.RawContent);
    }

    [Fact]
    public void SiteParams_ShouldThrowExceptionWhenSettingsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseParams(null!, typeof(Site), siteContentCONST));
    }

    [Fact]
    public void SiteParams_ShouldThrowExceptionWhenTypeIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseParams(siteDefault.Object, null!, siteContentCONST));
    }

    [Fact]
    public void SiteParams_ShouldHandleEmptyContent()
    {
        parser.ParseParams(siteDefault.Object, typeof(Site), string.Empty);
        Assert.Empty(siteDefault.Object.Params);
    }

    [Fact]
    public void SiteParams_ShouldPopulateParamsWithExtraFields()
    {
        parser.ParseParams(siteDefault.Object, typeof(Site), siteContentCONST);
        Assert.NotEmpty(siteDefault.Object.Params);
        Assert.True(siteDefault.Object.Params.ContainsKey("customParam"));
        Assert.Equal("Custom Value", siteDefault.Object.Params["customParam"]);
        Assert.Equal(new[] { "Test", "Real Data" }, ((Dictionary<object, object>)siteDefault.Object.Params["NestedData"])["Level2"]);
        Assert.Equal("Test", ((siteDefault.Object?.Params["NestedData"] as Dictionary<object, object>)?["Level2"] as List<object>)?[0]);
    }
}
