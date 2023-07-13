using Moq;
using Serilog.Events;
using SuCoS;
using Xunit;

namespace Test;

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
        var program = new Program(loggerMock.Object);

        // Act
        program.OutputLogo();
        program.OutputWelcome();

        // Assert
        loggerMock.Verify(x => x.Information(Program.helloWorld), Times.Once);
    }
}