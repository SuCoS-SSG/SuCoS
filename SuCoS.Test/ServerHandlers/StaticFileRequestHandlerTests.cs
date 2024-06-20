using NSubstitute;
using SuCoS.ServerHandlers;
using Xunit;

namespace test.ServerHandlers;

public class StaticFileRequestHandlerTests : TestSetup, IDisposable
{
    private readonly string _tempFilePath;

    public StaticFileRequestHandlerTests()
    {
        // Creating a temporary file for testing purposes
        _tempFilePath = Path.GetTempFileName();
        File.WriteAllText(_tempFilePath, "test");
    }

    [Fact]
    public void Check_ReturnsTrueForExistingFile()
    {
        // Arrange
        var requestPath = Path.GetFileName(_tempFilePath);
        var basePath = Path.GetDirectoryName(_tempFilePath)
            ?? throw new InvalidOperationException("Unable to determine directory of temporary file.");

        var staticFileRequest = new StaticFileRequest(basePath, false);

        // Act
        var result = staticFileRequest.Check(requestPath: requestPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Handle_ReturnsExpectedContent()
    {
        // Arrange
        var requestPath = Path.GetFileName(_tempFilePath);
        var basePath = Path.GetDirectoryName(_tempFilePath)
            ?? throw new InvalidOperationException("Unable to determine directory of temporary file.");
        var staticFileRequest = new StaticFileRequest(basePath, true);


        var response = Substitute.For<IHttpListenerResponse>();
        var stream = new MemoryStream();
        _ = response.OutputStream.Returns(stream);

        // Act
        _ = staticFileRequest.Check(requestPath);
        var code = await staticFileRequest.Handle(response, requestPath, DateTime.Now).ConfigureAwait(true);

        // Assert
        _ = stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync().ConfigureAwait(true);

        Assert.Equal("test", content);

        Assert.Equal("themeSt", code);
    }

    public void Dispose()
    {
        // Cleaning up the temporary file after tests run
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }

        GC.SuppressFinalize(this);
    }
}
