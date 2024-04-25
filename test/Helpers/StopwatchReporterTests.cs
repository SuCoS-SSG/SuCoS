
using Serilog;
using Serilog.Sinks.InMemory;
using SuCoS.Helpers;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Xunit;

namespace Tests.Helpers;

public class StopwatchReporterTests
{
    private readonly ILogger logger;
    private readonly InMemorySink inMemorySink;

    public StopwatchReporterTests()
    {
        inMemorySink = new InMemorySink();
        logger = new LoggerConfiguration().WriteTo.Sink(inMemorySink).CreateLogger();
    }

    [Fact]
    public void Start_InitializesAndStartsStopwatchForStep()
    {
        // Arrange
        var stepName = "TestStep";
        var stopwatchReporter = new StopwatchReporter(new LoggerConfiguration().CreateLogger());

        // Act
        stopwatchReporter.Start(stepName);

        // Assert
        var stopwatchField = stopwatchReporter.GetType().GetField("stopwatches", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(stopwatchField);

        var stopwatchDictionary = stopwatchField.GetValue(stopwatchReporter) as Dictionary<string, Stopwatch>;
        Assert.NotNull(stopwatchDictionary);

        Assert.True(stopwatchDictionary.ContainsKey(stepName));
        Assert.True(stopwatchDictionary[stepName].IsRunning);
    }

    [Fact]
    public void LogReport_CorrectlyLogsElapsedTime()
    {
        var stepName = "TestStep";
        var siteTitle = "TestSite";
        var duration = 123;

        var stopwatchReporter = new StopwatchReporter(logger);
        stopwatchReporter.Start(stepName);
        Thread.Sleep(duration); // Let's wait a bit to simulate some processing.
        stopwatchReporter.Stop(stepName, 1);

        stopwatchReporter.LogReport(siteTitle);

        // Assert
        var logEvents = inMemorySink.LogEvents;
        Assert.NotEmpty(logEvents);
        var logMessage = logEvents.First().RenderMessage(CultureInfo.InvariantCulture);
        Assert.Contains($"Site '{siteTitle}' created!", logMessage, StringComparison.InvariantCulture);
        Assert.Contains(stepName, logMessage, StringComparison.InvariantCulture);
        // Assert.Contains($"{duration} ms", logMessage, StringComparison.InvariantCulture); // Ensure that our processing time was logged.
    }

    [Fact]
    public void Stop_ThrowsExceptionWhenStopCalledWithoutStart()
    {
        var stepName = "TestStep";
        var stopwatchReporter = new StopwatchReporter(logger);

        // Don't call Start for stepName

        // Assert that Stop throws an exception
        var exception = Assert.Throws<ArgumentException>(() => stopwatchReporter.Stop(stepName, 1));
        Assert.Equal($"Step '{stepName}' has not been started.", exception.Message);
    }

}
