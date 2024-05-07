using SuCoS.Models;
using System.Globalization;
using Xunit;

namespace Tests.Models;

public class FrontMatterTests : TestSetup
{
    [Theory]
    [InlineData("Title1", "Section1", "Type1", "URL1", Kind.Single)]
    [InlineData("Title2", "Section2", "Type2", "URL2", Kind.List)]
    [InlineData("Title3", "Section3", "Type3", "URL3", Kind.Index)]
    public void Constructor_Sets_Properties_Correctly(string title, string section, string type, string url, Kind kind)
    {
        // Act
        var basicContent = new FrontMatter
        {
            Title = title,
            Section = section,
            Type = type,
            URL = url,
            Kind = kind
        };

        // Assert
        Assert.Equal(title, basicContent.Title);
        Assert.Equal(section, basicContent.Section);
        Assert.Equal(type, basicContent.Type);
        Assert.Equal(url, basicContent.URL);
        Assert.Equal(kind, basicContent.Kind);
    }

    [Theory]
    [InlineData("C:/Test/Document.txt", "Document")]
    [InlineData("C:/Test/SubFolder/Document.txt", "Document")]
    [InlineData("Document.txt", "Document")]
    public void SourceFileNameWithoutExtension_Returns_Correct_FileName(string sourcePath, string expectedFileName)
    {
        // Arrange
        var frontMatter = new FrontMatter("Title", sourcePath);

        // Act
        var actualFileName = frontMatter.SourceFileNameWithoutExtension;

        // Assert
        Assert.Equal(expectedFileName, actualFileName);
    }

    [Theory]
    [InlineData("C:/Test/Document.txt", "C:/Test")]
    [InlineData("C:/Test/SubFolder/Document.txt", "C:/Test/SubFolder")]
    [InlineData("/home/Test/Document.txt", "/home/Test")]
    [InlineData("/Test/SubFolder/Document.txt", "/Test/SubFolder")]
    [InlineData("Document.txt", "")]
    public void SourcePathDirectory_Returns_Correct_Directory(string sourcePath, string expectedDirectory)
    {
        // Arrange
        var frontMatter = new FrontMatter("Title", sourcePath);

        // Assert
        Assert.Equal(expectedDirectory, frontMatter.SourceRelativePathDirectory);
    }

    [Theory]
    [InlineData("2023-07-11T00:00:00", "2023-07-12T00:00:00", "2023-07-12T00:00:00")]
    [InlineData("2023-07-11T00:00:00", null, "2023-07-11T00:00:00")]
    [InlineData("2023-07-11", "2023-07-12", "2023-07-12")]
    [InlineData("2023-07-11", null, "2023-07-11")]
    [InlineData(null, null, null)]
    public void GetPublishDate_Returns_PublishDate_If_Not_Null_Otherwise_Date(string? dateString, string? publishDateString, string? expectedDateString)
    {
        // Arrange
        var date = string.IsNullOrEmpty(dateString) ? (DateTime?)null : DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        var publishDate = string.IsNullOrEmpty(publishDateString) ? (DateTime?)null : DateTime.Parse(publishDateString, CultureInfo.InvariantCulture);
        var expectedDate = string.IsNullOrEmpty(expectedDateString) ? (DateTime?)null : DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture);

        // Act
        var frontMatter = new FrontMatter { Date = date, PublishDate = publishDate };

        // Assert
        Assert.Equal(expectedDate, frontMatter.GetPublishDate);
        Assert.Equal(expectedDate, (frontMatter as IFrontMatter).GetPublishDate);
    }
}
