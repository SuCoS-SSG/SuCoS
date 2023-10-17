using NSubstitute;
using SuCoS.ServerHandlers;
using Xunit;

namespace Tests.ServerHandlers;

public class PingRequestHandlerTests : TestSetup
{
    [Fact]
    public async Task Handle_ReturnsServerStartupTimestamp()
    {
        // Arrange
        var response = Substitute.For<IHttpListenerResponse>();
        var stream = new MemoryStream();
        response.OutputStream.Returns(stream);

        var pingRequests = new PingRequests();

        // Act
        var code = await pingRequests.Handle(response, "ping", todayDate);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        Assert.Equal(todayDate.ToString("o"), content);

        Assert.Equal("ping", code);
    }

    [Theory]
    [InlineData("/ping", true)]
    [InlineData("ping", false)]
    [InlineData(null, false)]
    public void Check_HandlesVariousRequestPaths(string requestPath, bool expectedResult)
    {
        // Arrange
        var pingRequests = new PingRequests();

        // Act
        var result = pingRequests.Check(requestPath);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}

