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
}
