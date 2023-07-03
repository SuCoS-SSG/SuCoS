using Xunit;
using SuCoS.Helpers;

namespace Test.Helpers;

public class UrlizerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Urlize_NullOrEmptyText_ThrowsArgumentNullException(string text)
    {
        var result = Urlizer.Urlize(text);
        Assert.Equal("", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UrlizePath_NullPath_ReturnsEmptyString(string path)
    {
        var result = Urlizer.UrlizePath(path);

        Assert.Equal("", result);
    }

    [Theory]
    [InlineData("Hello, World!", '-', true, false, "hello-world")]
    [InlineData("Hello, World!", '_', true, false, "hello_world")]
    [InlineData("Hello, World!", '-', false, false, "Hello-World")]
    [InlineData("Hello.World", '-', true, false, "hello.world")]
    [InlineData("Hello.World", '-', true, true, "hello-world")]
    public void Urlize_ValidText_ReturnsExpectedResult(string text, char? replacementChar, bool lowerCase, bool replaceDot, string expectedResult)
    {
        var options = new UrlizerOptions { ReplacementChar = replacementChar, LowerCase = lowerCase, ReplaceDot = replaceDot };
        var result = Urlizer.Urlize(text, options);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("Documents/My Report.docx", '-', true, false, "documents/my-report.docx")]
    [InlineData("Documents/My Report.docx", '_', true, false, "documents/my_report.docx")]
    [InlineData("Documents/My Report.docx", '-', false, false, "Documents/My-Report.docx")]
    [InlineData("Documents/My Report.docx", '-', true, true, "documents/my-report-docx")]
    [InlineData("C:/Documents/My Report.docx", '_', true, true, "c/documents/my_report_docx")]
    [InlineData("Documents/My Report.docx", null, true, false, "documents/myreport.docx")]
    public void UrlizePath_ValidPath_ReturnsExpectedResult(string path, char? replacementChar, bool lowerCase, bool replaceDot, string expectedResult)
    {
        var options = new UrlizerOptions { ReplacementChar = replacementChar, LowerCase = lowerCase, ReplaceDot = replaceDot };
        var result = Urlizer.UrlizePath(path, options);

        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void Urlize_WithoutOptions_ReturnsExpectedResult()
    {
        const string text = "Hello, World!";
        var result = Urlizer.Urlize(text);

        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void UrlizePath_WithoutOptions_ReturnsExpectedResult()
    {
        const string path = "Documents/My Report.docx";
        var result = Urlizer.UrlizePath(path);

        Assert.Equal("documents/my-report.docx", result);
    }

    [Fact]
    public void Urlize_SpecialCharsInText_ReturnsOnlyHyphens()
    {
        const string text = "!@#$%^&*()";
        var result = Urlizer.Urlize(text);

        Assert.Equal("", result);
    }

    [Fact]
    public void UrlizePath_SpecialCharsInPath_ReturnsOnlyHyphens()
    {
        const string path = "/!@#$%^&*()/";
        var result = Urlizer.UrlizePath(path);

        Assert.Equal("/", result);
    }
}
