using System.Globalization;
using SuCoS.Models;
using Xunit;

namespace test.Models;

public class ContentSourceTests : TestSetup
{
    [Theory]
    [InlineData("C:/Test/Document.txt", "Document")]
    [InlineData("C:/Test/SubFolder/Document.txt", "Document")]
    [InlineData("Document.txt", "Document")]
    public void SourceFileNameWithoutExtension_Returns_Correct_FileName(string sourcePath, string expectedFileName)
    {
        // Arrange
        FrontMatter frontMatter = new("Title", sourcePath);
        ContentSource contentSource = new(sourcePath, frontMatter);

        // Act
        var actualFileName = (contentSource as IFile).SourceFileNameWithoutExtension;

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
        ContentSource contentSource = new(sourcePath, frontMatter);

        // Act
        var actualDirectory = (contentSource as IFile).SourceRelativePathDirectory;

        // Assert
        Assert.Equal(expectedDirectory, actualDirectory);
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
        FrontMatter frontMatter = new() { Date = date, PublishDate = publishDate };
        ContentSource contentSource = new(string.Empty, frontMatter);

        // Assert
        Assert.Equal(expectedDate, (contentSource as IContentSource).GetPublishDate);
        Assert.Equal(expectedDate, (contentSource as IContentSource).GetPublishDate);
    }
}
