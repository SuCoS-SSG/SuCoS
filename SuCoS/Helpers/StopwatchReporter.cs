using System.Diagnostics;
using System.Globalization;
using System.Text;
using Serilog;

namespace SuCoS.Helpers;

/// <summary>
///  This class is used to report the time taken to execute
///  The stopwatch is started
///  and stopped around parts of the code that we want to measure.
/// </summary>
public class StopwatchReporter
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, Stopwatch> _stopwatches = [];
    private readonly Dictionary<string, int> _itemCounts = [];

    /// <summary>
    /// Constructor
    /// </summary>
    public StopwatchReporter(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Start the stopwatch for the given step name.
    /// </summary>
    /// <param name="stepName"></param>
    public void Start(string stepName)
    {
        if (!_stopwatches.TryGetValue(stepName, out var stopwatch))
        {
            stopwatch = new Stopwatch();
            _stopwatches[stepName] = stopwatch;
        }

        stopwatch.Restart();
    }

    /// <summary>
    /// Stop the stopwatch for the given step name.
    /// </summary>
    /// <param name="stepName"></param>
    /// <param name="itemCount"></param>
    /// <exception cref="ArgumentException"></exception>
    public void Stop(string stepName, int itemCount)
    {
        if (!_stopwatches.TryGetValue(stepName, out var stopwatch))
        {
            throw new ArgumentException($"Step '{stepName}' has not been started.");
        }

        stopwatch.Stop();
        _itemCounts[stepName] = itemCount;
    }

    /// <summary>
    /// Generate a report of the time taken for each step.
    /// </summary>
    /// <param name="siteTitle"></param>
    public void LogReport(string siteTitle)
    {
        var reportData = new List<(string Step, string Status, string DurationString, long Duration)>
        {
            ("Step", "Status", "Duration", 0)
        };

        foreach (var (stepName, stopwatch) in _stopwatches)
        {
            _ = _itemCounts.TryGetValue(stepName, out var itemCount);
            var duration = stopwatch.ElapsedMilliseconds;
            var durationString = $"{duration} ms";
            var status = itemCount > 0 ? itemCount.ToString(CultureInfo.InvariantCulture) : string.Empty;

            reportData.Add((Step: stepName, Status: status, DurationString: durationString, Duration: duration));
        }

        var totalDurationAllSteps = _stopwatches.Values.Sum(sw => sw.ElapsedMilliseconds);

        var report = new StringBuilder($@"Site '{siteTitle}' created!
═════════════════════════════════════════════");

        for (var i = 0; i < reportData.Count; i++)
        {
            if (i == 1 || i == reportData.Count)
            {
                report.Append(@"
─────────────────────────────────────────────");
            }
            report.Append(CultureInfo.InvariantCulture,
                $"\n{reportData[i].Step,-20} {reportData[i].Status,-15} {reportData[i].DurationString,-10}");
        }

        report.Append(CultureInfo.InvariantCulture,
            $@"
─────────────────────────────────────────────
Total                     {totalDurationAllSteps} ms
═════════════════════════════════════════════");

        // Log the report
        _logger.Information(report.ToString(), siteTitle);
    }
}
