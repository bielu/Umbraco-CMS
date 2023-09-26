using Serilog.Core;
using Serilog.Events;

namespace Umbraco.Cms.Infrastructure.Logging.Serilog.Sinks;

public class UmbracoSink : ILogEventSink
{
    private readonly IUmbracoSinkProvider _sinkProvider;

    public UmbracoSink(IUmbracoSinkProvider sinkProvider)
    {
        _sinkProvider = sinkProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        _sinkProvider.Emit(logEvent);
    }
}
