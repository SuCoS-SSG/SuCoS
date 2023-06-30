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
    private readonly string fileContent = @"---
Title: Real Data Title
Date: 2023-07-01
Categories: ['Test', 'Real Data']
Tags: ['Test', 'Real Data']
---

# Real Data Test

This is a test using real data. Real Data Test
";

    public YAMLParserTests()
    {
        parser = new YAMLParser();
        site = new Mock<Site>();
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
---
", null)]
    public void ParseFrontmatter_ShouldParseTitleCorrectly(string fileContent, string expectedTitle)
    {
        var filePath = "test.md";

        // Act
        var frontmatter = parser.ParseFrontmatter(site.Object, filePath, fileContent);

        // Asset
        Assert.Equal(expectedTitle, frontmatter?.Title);
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowException_WhenSiteIsNull()
    {
        var fileContent = @"---
Title: Test Title
---
";

        // Asset
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatter(null!, "test.md", fileContent));
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
        var frontmatter = parser.ParseFrontmatter(site.Object, filePath, fileContent);

        // Asset
        Assert.Equal(expectedDate, frontmatter?.Date);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseOtherFieldsCorrectly()
    {
        var filePath = "test.md";
        var fileContent = @"---
Title: Test Title
Type: post
Date: 2023-01-01
LastMod: 2023-06-01
PublishDate: 2023-06-01
ExpiryDate: 2024-06-01
---
";
        var expectedDate = DateTime.Parse("2023-01-01", CultureInfo.InvariantCulture);
        var expectedLastMod = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedPublishDate = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedExpiryDate = DateTime.Parse("2024-06-01", CultureInfo.InvariantCulture);

        // Act
        var frontmatter = parser.ParseFrontmatter(site.Object, filePath, fileContent);

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
        Assert.Throws<YamlDotNet.Core.YamlException>(() => parser.ParseFrontmatter(site.Object, filePath, fileContent));
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnSiteWithCorrectSettings()
    {
        var siteContent = @"
BaseUrl: https://www.example.com/
Title: My Site
";

        // Act
        var siteSettings = parser.ParseSiteSettings(siteContent);

        // Asset
        Assert.Equal("https://www.example.com/", siteSettings.BaseUrl);
        Assert.Equal("My Site", siteSettings.Title);
    }

    [Fact]
    public void ParseParams_ShouldFillParamsWithNonMatchingFields()
    {
        var settings = new Frontmatter("Test Title", "/test.md", site.Object);
        var content = @"
Title: Test Title
customParam: Custom Value
";

        // Act
        parser.ParseParams(settings, typeof(Frontmatter), content);

        // Asset
        Assert.True(settings.Params.ContainsKey("customParam"));
        Assert.Equal("Custom Value", settings.Params["customParam"]);
    }


    [Fact]
    public void ParseFrontmatter_ShouldParseContentInSiteFolder()
    {
        var frontmatter = parser.ParseFrontmatter(site.Object, "", fileContent);

        // Act
        site.Object.PostProcessFrontMatter(frontmatter!);

        // Asset
        Assert.Equal(DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture), frontmatter?.Date);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseCategoriesCorrectly()
    {
        var frontmatter = parser.ParseFrontmatter(site.Object, "", fileContent);

        // Act
        site.Object.PostProcessFrontMatter(frontmatter!);

        // Asset
        Assert.Equal(new[] { "Test", "Real Data" }, frontmatter?.Params["Categories"]);
    }
}
