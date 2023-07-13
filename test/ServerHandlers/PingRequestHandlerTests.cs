using Microsoft.AspNetCore.Http;
using SuCoS.ServerHandlers;
using Xunit;

namespace Tests.ServerHandlers;

public class PingRequestHandlerTests : TestSetup
{
    [Fact]
    public async Task Handle_ReturnsServerStartupTimestamp()
    {
        // Arrange
        var context = new DefaultHttpContext();

        var stream = new MemoryStream();
        context.Response.Body = stream;

        var pingRequests = new PingRequests();

        // Act
        await pingRequests.Handle(context, "ping", todayDate);

        // Assert
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        Assert.Equal(todayDate.ToString("o"), content);
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

