using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Compact.Reader;
using Serilog.Sinks.File;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Logging.Viewer;

namespace Umbraco.Cms.Infrastructure.Logging.Serilog.Sinks;

public class UmbracoFileSinkProvider : IUmbracoSinkProvider
{
    private const int FileSizeCap = 100;
    private readonly UmbracoFileConfiguration _configuration;
    private readonly ILoggingConfiguration _loggingConfiguration;
    //todo: consider merging configurations
    public UmbracoFileSinkProvider(IOptionsMonitor<UmbracoFileConfiguration> configuration, ILoggingConfiguration loggingConfiguration)
    {
        _configuration = configuration.CurrentValue;
        _loggingConfiguration = loggingConfiguration;
        this.FileSink = new FileSink(_configuration.GetPath(loggingConfiguration.LogDirectory),
            _configuration.TextFormatter ?? new CompactJsonFormatter(),
            _configuration.FileSizeLimitBytes, _configuration.Encoding, _configuration.Buffered);
    }

    private FileSink FileSink { get; set; }

    public void Emit(LogEvent logEvent)
    {
        this.FileSink.Emit(logEvent);
    }

    public bool CanCheckLogsForTimePeriod(LogTimePeriod logTimePeriod)
    {
        // Log Directory
        var logDirectory = _loggingConfiguration.LogDirectory;

        // Number of entries
        long fileSizeCount = 0;

        // foreach full day in the range - see if we can find one or more filenames that end with
        // yyyyMMdd.json - Ends with due to MachineName in filenames - could be 1 or more due to load balancing
        for (DateTime day = logTimePeriod.StartTime.Date; day.Date <= logTimePeriod.EndTime.Date; day = day.AddDays(1))
        {
            // Filename ending to search for (As could be multiple)
            var filesToFind = GetSearchPattern(day);

            var filesForCurrentDay = Directory.GetFiles(logDirectory, filesToFind);

            fileSizeCount += filesForCurrentDay.Sum(x => new FileInfo(x).Length);
        }

        // The GetLogSize call on JsonLogViewer returns the total file size in bytes
        // Check if the log size is not greater than 100Mb (FileSizeCap)
        var logSizeAsMegabytes = fileSizeCount / 1024 / 1024;
        return logSizeAsMegabytes <= FileSizeCap;
    }

    public  IReadOnlyList<LogEvent> GetLogs(LogTimePeriod logTimePeriod, ILogFilter filter, int skip, int take)
    {
        var logs = new List<LogEvent>();

        var count = 0;

        // foreach full day in the range - see if we can find one or more filenames that end with
        // yyyyMMdd.json - Ends with due to MachineName in filenames - could be 1 or more due to load balancing
        for (DateTime day = logTimePeriod.StartTime.Date; day.Date <= logTimePeriod.EndTime.Date; day = day.AddDays(1))
        {
            // Filename ending to search for (As could be multiple)
            var filesToFind = GetSearchPattern(day);

            var filesForCurrentDay = Directory.GetFiles(_loggingConfiguration.LogDirectory, filesToFind);

            // Foreach file we find - open it
            foreach (var filePath in filesForCurrentDay)
            {
                // Open log file & add contents to the log collection
                // Which we then use LINQ to page over
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var stream = new StreamReader(fs))
                    {
                        var reader = new LogEventReader(stream);
                        while (TryRead(reader, out LogEvent? evt))
                        {
                            // We may get a null if log line is malformed
                            if (evt == null)
                            {
                                continue;
                            }

                            if (count > skip + take)
                            {
                                break;
                            }

                            if (count < skip)
                            {
                                count++;
                                continue;
                            }

                            if (filter.TakeLogEvent(evt))
                            {
                                logs.Add(evt);
                            }

                            count++;
                        }
                    }
                }
            }
        }

        return logs;
    }
    private bool TryRead(LogEventReader reader, out LogEvent? evt)
    {
            return reader.TryRead(out evt);
    }
    public string GetSearchPattern(DateTime day) => $"*{day:yyyyMMdd}*.json";
}
