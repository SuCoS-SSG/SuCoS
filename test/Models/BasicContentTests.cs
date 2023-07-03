using SuCoS.Models;
using Xunit;

namespace Test.Models;

public class BasicContentTests
{
    [Theory]
    [InlineData("Title1", "Section1", "Type1", "URL1", Kind.single)]
    [InlineData("Title2", "Section2", "Type2", "URL2", Kind.list)]
    [InlineData("Title3", "Section3", "Type3", "URL3", Kind.index)]
    public void Constructor_Sets_Properties_Correctly(string title, string section, string type, string url, Kind kind)
    {
        // Act
        var basicContent = new BasicContent(title, section, type, url, kind);

        // Assert
        Assert.Equal(title, basicContent.Title);
        Assert.Equal(section, basicContent.Section);
        Assert.Equal(type, basicContent.Type);
        Assert.Equal(url, basicContent.URL);
        Assert.Equal(kind, basicContent.Kind);
    }

    [Fact]
    public void Constructor_Sets_Kind_To_List_If_Not_Provided()
    {
        // Arrange
        const string title = "Title1", section = "Section1", type = "Type1", url = "URL1";

        // Act
        var basicContent = new BasicContent(title, section, type, url);

        // Assert
        Assert.Equal(Kind.list, basicContent.Kind);
    }
}
