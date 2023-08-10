using Xunit;
using SuCoS.Parser;
using System.Globalization;
using SuCoS.Helpers;
using SuCoS.Models;

namespace Test.Parser;

public class YAMLParserTests : TestSetup
{
    private readonly YAMLParser parser;

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
BaseURL: https://www.example.com/
Description: Tastiest C# Static Site Generator of the World
Copyright: Copyright message
customParam: Custom Value
NestedData:
  Level2:
    - Test
    - Real Data
";
    private const string fileFullPathCONST = "test.md";
    private const string fileRelativePathCONST = "test.md";
    private readonly string pageContent;

    public YAMLParserTests()
    {
        parser = new YAMLParser();
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
        var frontMatter = parser.ParseFrontmatterAndMarkdown(fileRelativePathCONST, fileFullPathCONST, fileContent);

        // Asset
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
        var expectedDate = DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture);

        // Act
        var frontMatter = parser.ParseFrontmatterAndMarkdown(fileRelativePathCONST, fileFullPathCONST, fileContent);

        // Asset
        Assert.Equal(expectedDate, frontMatter.Date);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseOtherFieldsCorrectly()
    {
        var expectedDate = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
        var expectedLastMod = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedPublishDate = DateTime.Parse("2023-06-01", CultureInfo.InvariantCulture);
        var expectedExpiryDate = DateTime.Parse("2024-06-01", CultureInfo.InvariantCulture);

        // Act
        var frontMatter = parser.ParseFrontmatterAndMarkdown(fileRelativePathCONST, fileFullPathCONST, pageContent);

        // Asset
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
        const string fileContent = @"---
Title
---
";

        // Asset
        Assert.Throws<YamlDotNet.Core.YamlException>(() => parser.ParseFrontmatterAndMarkdown(fileRelativePathCONST, fileFullPathCONST, fileContent));
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnSiteWithCorrectSettings()
    {
        // Act
        var site = parser.ParseSiteSettings(siteContentCONST);


        // Asset
        Assert.Equal("My Site", site.Title);
        Assert.Equal("https://www.example.com/", site.BaseURL);
        Assert.Equal("Tastiest C# Static Site Generator of the World", site.Description);
        Assert.Equal("Copyright message", site.Copyright);
    }

    [Fact]
    public void ParseParams_ShouldFillParamsWithNonMatchingFields()
    {
        var page = new Page(new FrontMatter
        {
            Title = "Test Title",
            SourceRelativePath = "/test.md"
        }, site);

        // Act
        parser.ParseParams(page, typeof(Page), pageFrontmaterCONST);

        // Asset
        Assert.True(page.Params.ContainsKey("customParam"));
        Assert.Equal("Custom Value", page.Params["customParam"]);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseContentInSiteFolder()
    {
        var date = DateTime.Parse("2023-07-01", CultureInfo.InvariantCulture);
        var frontMatter = parser.ParseFrontmatterAndMarkdown("", "", pageContent);
        Page page = new(frontMatter, site);

        // Act
        site.PostProcessPage(page);

        // Asset
        Assert.Equal(date, frontMatter.Date);
    }

    [Fact]
    public void ParseFrontmatter_ShouldCreateTags()
    {
        // Act
        var frontMatter = parser.ParseFrontmatterAndMarkdown("", "", pageContent);
        Page page = new(frontMatter, site);

        // Act
        site.PostProcessPage(page);

        // Asset
        Assert.Equal(2, page.TagsReference.Count);
    }

    [Fact]
    public void ParseFrontmatter_ShouldParseCategoriesCorrectly()
    {
        var frontMatter = parser.ParseFrontmatterAndMarkdown("fakeFilePath", "/fakeFilePath", pageContent);

        // Asset
        Assert.Equal(new[] { "Test", "Real Data" }, frontMatter.Params["Categories"]);
        Assert.Equal(new[] { "Test", "Real Data" }, (frontMatter.Params["NestedData"] as Dictionary<object, object>)?["Level2"]);
        Assert.Equal("Test", ((frontMatter.Params["NestedData"] as Dictionary<object, object>)?["Level2"] as List<object>)?[0]);
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenSiteIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdownFromFile(null!, "fakeFilePath"));
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdownFromFile(null!));
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() => parser.ParseFrontmatterAndMarkdownFromFile("fakePath"));
    }

    [Fact]
    public void ParseFrontmatter_ShouldThrowExceptionWhenFilePathDoesNotExist2()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseFrontmatterAndMarkdown(null!, null!, "fakeContent"));
    }

    [Fact]
    public void ParseFrontmatter_ShouldHandleEmptyFileContent()
    {
        Assert.Throws<FormatException>(() => parser.ParseFrontmatterAndMarkdown("fakeFilePath", "/fakeFilePath", ""));
    }

    [Fact]
    public void ParseYAML_ShouldThrowExceptionWhenFrontmatterIsInvalid()
    {
        Assert.Throws<FormatException>(() => parser.ParseFrontmatterAndMarkdown("fakeFilePath", "/fakeFilePath", "invalidFrontmatter"));
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnSiteSettings()
    {
        var site = parser.ParseSiteSettings(siteContentCONST);
        Assert.NotNull(site);
        Assert.Equal("My Site", site.Title);
        Assert.Equal("https://www.example.com/", site.BaseURL);
    }

    [Fact]
    public void ParseSiteSettings_ShouldReturnContent()
    {
        var frontMatter = parser.ParseFrontmatterAndMarkdown("fakeFilePath", "/fakeFilePath", pageContent);

        Assert.Equal(pageMarkdownCONST, frontMatter.RawContent);
    }

    [Fact]
    public void SiteParams_ShouldThrowExceptionWhenSettingsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseParams(null!, typeof(Site), siteContentCONST));
    }

    [Fact]
    public void SiteParams_ShouldThrowExceptionWhenTypeIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => parser.ParseParams(site, null!, siteContentCONST));
    }

    [Fact]
    public void SiteParams_ShouldHandleEmptyContent()
    {
        parser.ParseParams(site, typeof(Site), string.Empty);
        Assert.Empty(site.Params);
    }

    [Fact]
    public void SiteParams_ShouldPopulateParamsWithExtraFields()
    {
        parser.ParseParams(site, typeof(Site), siteContentCONST);
        Assert.NotEmpty(site.Params);
        Assert.True(site.Params.ContainsKey("customParam"));
        Assert.Equal("Custom Value", site.Params["customParam"]);
        Assert.Equal(new[] { "Test", "Real Data" }, ((Dictionary<object, object>)site.Params["NestedData"])["Level2"]);
        Assert.Equal("Test", ((site.Params["NestedData"] as Dictionary<object, object>)?["Level2"] as List<object>)?[0]);
    }
}
