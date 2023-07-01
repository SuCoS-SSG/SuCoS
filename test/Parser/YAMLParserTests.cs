using Xunit;
using Moq;
using SuCoS.Models;
using SuCoS.Parser;
using System.Globalization;
using SuCoS.Helper;

namespace SuCoS.Tests;

public class YAMLParserTests
{
    private readonly YAMLParser parser;
    private readonly Mock<Site> site;

    private readonly string pageFrontmater = @"Title: Test Title
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
    private readonly string pageMarkdown = @"
# Real Data Test

This is a test using real data. Real Data Test
";
    private readonly string siteContent = @"
Title: My Site
BaseUrl: https://www.example.com/
customParam: Custom Value
NestedData:
  Level2:
    - Test
    - Real Data
";
    private readonly string pageContent;

    public YAMLParserTests()
    {
        parser = new YAMLParser();
        site = new Mock<Site>();
        pageContent = @$"---
{pageFrontmater}
---
{pageMarkdown}";
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
        var filePath = "test.md";

        // Act
        var frontmatter = parser.ParseFrontmatterAndMarkdown(site.Object, filePath, fileContent);

        // Asset
        Assert.Equal(expectedTitle, frontmatter?.Title);
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowException_WhenSiteIsNull()
    {
        // Asset
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdown(null!, "test.md", pageContent));
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
        var filePath = "test.md";
        var expectedDate = DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture);

        // Act
        var frontmatter = parser.ParseFrontmatterAndMarkdown(site.Object, filePath, fileContent);

        // Asset
        Assert.Equal(expectedDate, frontmatter?.Date);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseOtherFieldsCorrectly()
    {
        var filePath = "test.md";
        var expectedDate = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
        var expectedLastMod = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedPublishDate = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedExpiryDate = DateTime.Parse("2024-06-01", CultureInfo.InvariantCulture);

        // Act
        var frontmatter = parser.ParseFrontmatterAndMarkdown(site.Object, filePath, pageContent);

        // Asset
        Assert.Equal("Test Title", frontmatter?.Title);
        Assert.Equal("post", frontmatter?.Type);
        Assert.Equal(expectedDate, frontmatter?.Date);
        Assert.Equal(expectedLastMod, frontmatter?.LastMod);
        Assert.Equal(expectedPublishDate, frontmatter?.PublishDate);
        Assert.Equal(expectedExpiryDate, frontmatter?.ExpiryDate);
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowException_WhenInvalidYAMLSyntax()
    {
        var fileContent = @"---
Title
---
";
        var filePath = "test.md";

        // Asset
        Assert.Throws<YamlDotNet.Core.YamlException>(() => parser.ParseFrontmatterAndMarkdown(site.Object, filePath, fileContent));
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnSiteWithCorrectSettings()
    {
        // Act
        var siteSettings = parser.ParseSiteSettings(siteContent);

        // Asset
        Assert.Equal("https://www.example.com/", siteSettings.BaseUrl);
        Assert.Equal("My Site", siteSettings.Title);
    }

    [Fact]
    public void ParseParams_ShouldFillParamsWithNonMatchingFields()
    {
        var page = new Frontmatter("Test Title", "/test.md", site.Object);

        // Act
        parser.ParseParams(page, typeof(Frontmatter), pageFrontmater);

        // Asset
        Assert.True(page.Params.ContainsKey("customParam"));
        Assert.Equal("Custom Value", page.Params["customParam"]);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseContentInSiteFolder()
    {
        var date = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
        var frontmatter = parser.ParseFrontmatterAndMarkdown(site.Object, "", pageContent);

        // Act
        site.Object.PostProcessFrontMatter(frontmatter!);

        // Asset
        Assert.Equal(date, frontmatter?.Date);
    }

    [Fact]
    public void ParseFrontmatter_ShouldCreateTags()
    {
        var frontmatter = parser.ParseFrontmatterAndMarkdown(site.Object, "", pageContent);

        // Asset
        Assert.Equal(2, frontmatter!.Tags?.Count);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseCategoriesCorrectly()
    {
        var frontmatter = parser.ParseFrontmatterAndMarkdown(site.Object, "fakeFilePath", pageContent);

        // Asset
        Assert.Equal(new[] { "Test", "Real Data" }, frontmatter?.Params["Categories"]);
        Assert.Equal(new[] { "Test", "Real Data" }, (frontmatter?.Params["NestedData"] as Dictionary<object, object>)?["Level2"]);
        Assert.Equal("Test", ((frontmatter?.Params["NestedData"] as Dictionary<object, object>)?["Level2"] as List<object>)?[0]);
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenSiteIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdownFromFile(null!, "fakeFilePath"));
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdownFromFile(site.Object, null!));
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() => parser.ParseFrontmatterAndMarkdownFromFile(site.Object, "fakePath"));
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathDoesNotExist2()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdown(site.Object, null!, "fakeContent"));
    }

    [Fact]
    public void ParseFrontmatter_ShouldHandleEmptyFileContent()
    {
        Assert.Throws<FormatException>(() => parser.ParseFrontmatterAndMarkdown(site.Object, "fakeFilePath", ""));
    }

    [Fact]
    public void ParseYAML_ShouldThrowExceptionWhenFrontmatterIsInvalid()
    {
        Assert.Throws<FormatException>(() => parser.ParseFrontmatterAndMarkdown(site.Object, "fakeFilePath", "invalidFrontmatter"));
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnSiteSettings()
    {
        var site = parser.ParseSiteSettings(siteContent);
        Assert.NotNull(site);
        Assert.Equal("My Site", site.Title);
        Assert.Equal("https://www.example.com/", site.BaseUrl);
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnContent()
    {
        var frontmatter = parser.ParseFrontmatterAndMarkdown(site.Object, "fakeFilePath", pageContent);

        Assert.Equal(pageMarkdown, frontmatter?.RawContent);
    }

    [Fact]
    public void SiteParams_ShouldThrowExceptionWhenSettingsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseParams(null!, typeof(Site), siteContent));
    }

    [Fact]
    public void SiteParams_ShouldThrowExceptionWhenTypeIsNull()
    {
        var site = new Site();
        Assert.Throws<ArgumentNullException>(() => parser.ParseParams(site, null!, siteContent));
    }

    [Fact]
    public void SiteParams_ShouldHandleEmptyContent()
    {
        parser.ParseParams(site.Object, typeof(Site), string.Empty);
        Assert.Empty(site.Object.Params);
    }

    [Fact]
    public void SiteParams_ShouldPopulateParamsWithExtraFields()
    {
        parser.ParseParams(site.Object, typeof(Site), siteContent);
        Assert.NotEmpty(site.Object.Params);
        Assert.True(site.Object.Params.ContainsKey("customParam"));
        Assert.Equal("Custom Value", site.Object.Params["customParam"]);
        Assert.Equal(new[] { "Test", "Real Data" }, ((Dictionary<object, object>)site.Object.Params["NestedData"])["Level2"]);
        Assert.Equal("Test", ((site.Object?.Params["NestedData"] as Dictionary<object, object>)?["Level2"] as List<object>)?[0]);
    }
}
