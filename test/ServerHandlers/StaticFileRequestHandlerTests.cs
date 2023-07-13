using Microsoft.AspNetCore.Http;
using SuCoS.ServerHandlers;
using Xunit;

namespace Tests.ServerHandlers;

public class StaticFileRequestHandlerTests : TestSetup, IDisposable
{
    private readonly string tempFilePath;

    public StaticFileRequestHandlerTests() : base()
    {
        // Creating a temporary file for testing purposes
        tempFilePath = Path.GetTempFileName();
        File.WriteAllText(tempFilePath, "test");
    }

    [Fact]
    public void Check_ReturnsTrueForExistingFile()
    {
        // Arrange
        var requestPath = Path.GetFileName(tempFilePath);
        var basePath = Path.GetDirectoryName(tempFilePath)
            ?? throw new InvalidOperationException("Unable to determine directory of temporary file.");

        var staticFileRequest = new StaticFileRequest(basePath, false);

        // Act
        var result = staticFileRequest.Check(requestPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Handle_ReturnsExpectedContent()
    {
        // Arrange
        var requestPath = Path.GetFileName(tempFilePath);
        var basePath = Path.GetDirectoryName(tempFilePath)
            ?? throw new InvalidOperationException("Unable to determine directory of temporary file.");
        var staticFileRequest = new StaticFileRequest(basePath, true);

        var context = new DefaultHttpContext();
        var stream = new MemoryStream();
        context.Response.Body = stream;

        // Act
        staticFileRequest.Check(requestPath);
        await staticFileRequest.Handle(context, requestPath, DateTime.Now);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        Assert.Equal("test", content);
    }

    public void Dispose()
    {
        // Cleaning up the temporary file after tests run
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }
    }
}
