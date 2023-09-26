using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Formatting.Compact.Reader;
using Umbraco.Cms.Infrastructure.Logging.Serilog.Sinks;

namespace Umbraco.Cms.Core.Logging.Viewer;

internal class SerilogJsonLogViewer : SerilogLogViewerSourceBase
{
    private readonly IUmbracoSinkProvider _sinkProvider;
    private readonly ILogger<SerilogJsonLogViewer> _logger;

    public SerilogJsonLogViewer(
        IUmbracoSinkProvider sinkProvider,
        ILogger<SerilogJsonLogViewer> logger,
        ILogViewerConfig logViewerConfig,
        ILogLevelLoader logLevelLoader)
        : base(logViewerConfig, logLevelLoader)
    {
        _sinkProvider = sinkProvider;
        _logger = logger;
    }

    public override bool CanHandleLargeLogs => false;

    public override bool CheckCanOpenLogs(LogTimePeriod logTimePeriod)
    {
        return _sinkProvider.CanCheckLogsForTimePeriod(logTimePeriod);
    }

    protected override IReadOnlyList<LogEvent> GetLogs(LogTimePeriod logTimePeriod, ILogFilter filter, int skip,
        int take)
    {
        try
        {
            return _sinkProvider.GetLogs(logTimePeriod, filter, skip, take);
        }
        catch (JsonReaderException ex)
        {
            // As we are reading/streaming one line at a time in the JSON file
            // Thus we can not report the line number, as it will always be 1
            _logger.LogError(ex, "Unable to parse a line in the JSON log file");
            return new List<LogEvent>();
        }
    }
}
