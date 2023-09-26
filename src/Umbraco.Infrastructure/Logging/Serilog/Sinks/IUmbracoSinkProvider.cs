using Serilog.Core;
using Serilog.Events;
using Umbraco.Cms.Core.Logging.Viewer;

namespace Umbraco.Cms.Infrastructure.Logging.Serilog.Sinks;

public interface IUmbracoSinkProvider : ILogEventSink
{
    bool CanCheckLogsForTimePeriod(LogTimePeriod logTimePeriod);
    IReadOnlyList<LogEvent> GetLogs(LogTimePeriod logTimePeriod, ILogFilter filter, int skip, int take);
    public string GetSearchPattern(DateTime day);
}
