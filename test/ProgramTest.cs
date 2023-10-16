using NSubstitute;
using Serilog.Events;
using SuCoS;
using Xunit;

namespace Tests;

public class ProgramTests : TestSetup
{
    [Theory]
    [InlineData(false, LogEventLevel.Information)]
    [InlineData(true, LogEventLevel.Debug)]
    public void CreateLogger_SetsLogLevel(bool verbose, LogEventLevel expected)
    {
        // Act
        var logger = Program.CreateLogger(verbose);

        // Assert
        Assert.True(logger.IsEnabled(expected));
    }

    [Fact]
    public void OutputLogo_Should_LogHelloWorld()
    {
        // Arrange
        var program = new Program(loggerMock);

        // Act
        program.OutputLogo();
        program.OutputWelcome();

        // Assert
        loggerMock.Received(1).Information(Program.helloWorld);
    }
}
