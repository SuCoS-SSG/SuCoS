using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Serilog;
using Serilog.Sinks.InMemory;
using SuCoS.Helpers;
using Xunit;

namespace SuCoS.Test.Helpers;

public class StopwatchReporterTests
{
    private readonly ILogger _logger;
    private readonly InMemorySink _inMemorySink;

    public StopwatchReporterTests()
    {
        _inMemorySink = new InMemorySink();
        _logger = new LoggerConfiguration().WriteTo.Sink(_inMemorySink).CreateLogger();
    }

    [Fact]
    public void Start_InitializesAndStartsStopwatchForStep()
    {
        // Arrange
        const string stepName = "TestStep";
        var stopwatchReporter = new StopwatchReporter(new LoggerConfiguration().CreateLogger());

        // Act
        stopwatchReporter.Start(stepName);

        // Assert
        var stopwatchField = stopwatchReporter.GetType().GetField("_stopwatches", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(stopwatchField);

        var stopwatchDictionary = stopwatchField.GetValue(stopwatchReporter) as Dictionary<string, Stopwatch>;
        Assert.NotNull(stopwatchDictionary);

        Assert.True(stopwatchDictionary.ContainsKey(stepName));
        Assert.True(stopwatchDictionary[stepName].IsRunning);
    }

    [Fact]
    public void LogReport_CorrectlyLogsElapsedTime()
    {
        const string stepName = "TestStep";
        const string siteTitle = "TestSite";
        const int duration = 123;

        var stopwatchReporter = new StopwatchReporter(_logger);
        stopwatchReporter.Start(stepName);
        Thread.Sleep(duration); // Let's wait a bit to simulate some processing.
        stopwatchReporter.Stop(stepName, 1);

        stopwatchReporter.LogReport(siteTitle);

        // Assert
        var logEvents = _inMemorySink.LogEvents;
        var logEventsList = logEvents.ToList();
        Assert.NotEmpty(logEventsList);
        var logMessage = logEventsList.First().RenderMessage(CultureInfo.InvariantCulture);
        Assert.Contains($"Site '{siteTitle}' created!", logMessage, StringComparison.InvariantCulture);
        Assert.Contains(stepName, logMessage, StringComparison.InvariantCulture);
        // Assert.Contains($"{duration} ms", logMessage, StringComparison.InvariantCulture); // Ensure that our processing time was logged.
    }

    [Fact]
    public void Stop_ThrowsExceptionWhenStopCalledWithoutStart()
    {
        const string stepName = "TestStep";
        var stopwatchReporter = new StopwatchReporter(_logger);

        // Don't call Start for stepName

        // Assert that Stop throws an exception
        var exception = Assert.Throws<ArgumentException>(() => stopwatchReporter.Stop(stepName, 1));
        Assert.Equal($"Step '{stepName}' has not been started.", exception.Message);
    }

}
