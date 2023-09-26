using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace Umbraco.Cms.Infrastructure.Logging.Serilog;

public class UmbracoFileConfiguration
{

    public LogEventLevel RestrictedToMinimumLevel { get; set; } = LogEventLevel.Verbose;

    public long FileSizeLimitBytes { get; set; } = 1073741824;

    public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;

    public TimeSpan? FlushToDiskInterval { get; set; }

    public bool RollOnFileSizeLimit { get; set; }

    public int RetainedFileCountLimit { get; set; } = 31;
    public ITextFormatter? TextFormatter { get; set; } = new CompactJsonFormatter();
    public Encoding? Encoding { get; set; }
    public bool Buffered { get; set; }

    public string GetPath(string logDirectory) =>
        Path.Combine(logDirectory, $"UmbracoTraceLog.{Environment.MachineName}..json");
}
