using Serilog;
using System.Diagnostics;
using System.Globalization;

namespace SuCoS.Helpers;

/// <summary>
///  This class is used to report the time taken to execute
///  The stopwatch is started
///  and stopped around parts of the code that we want to measure.
/// </summary>
public class StopwatchReporter
{
    private readonly ILogger logger;
    private readonly Dictionary<string, Stopwatch> stopwatches;
    private readonly Dictionary<string, int> itemCounts;

    /// <summary>
    /// Constructor
    /// </summary>
    public StopwatchReporter(ILogger logger)
    {
        this.logger = logger;
        stopwatches = new Dictionary<string, Stopwatch>();
        itemCounts = new Dictionary<string, int>();
    }

    /// <summary>
    /// Start the stopwatch for the given step name.
    /// </summary>
    /// <param name="stepName"></param>
    public void Start(string stepName)
    {
        if (!stopwatches.TryGetValue(stepName, out var stopwatch))
        {
            stopwatch = new Stopwatch();
            stopwatches[stepName] = stopwatch;
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
        if (!stopwatches.TryGetValue(stepName, out var stopwatch))
        {
            throw new ArgumentException($"Step '{stepName}' has not been started.");
        }

        stopwatch.Stop();
        itemCounts[stepName] = itemCount;
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

        foreach (var (stepName, stopwatch) in stopwatches)
        {
            _ = itemCounts.TryGetValue(stepName, out var itemCount);
            var duration = stopwatch.ElapsedMilliseconds;
            var durationString = $"{duration} ms";
            var status = itemCount > 0 ? itemCount.ToString(CultureInfo.InvariantCulture) : "";

            reportData.Add((Step: stepName, Status: status, DurationString: durationString, Duration: duration));
        }

        var totalDurationAllSteps = stopwatches.Values.Sum(sw => sw.ElapsedMilliseconds);

        var report = $@"Site '{siteTitle}' created!
═════════════════════════════════════════════";

        for (var i = 0; i < reportData.Count; i++)
        {
            if (i == 1 || i == reportData.Count)
            {
                report += @"
─────────────────────────────────────────────";
            }
            report += $"\n{reportData[i].Step,-20} {reportData[i].Status,-15} {reportData[i].DurationString,-10}";
        }

        report += $@"
─────────────────────────────────────────────
Total                     {totalDurationAllSteps} ms
═════════════════════════════════════════════";

        // Log the report
        logger.Information(report, siteTitle);
    }
}
