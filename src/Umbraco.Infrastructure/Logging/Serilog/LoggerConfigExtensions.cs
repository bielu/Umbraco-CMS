using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Extensions;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Logging.Serilog.Enrichers;
using Umbraco.Cms.Infrastructure.Logging.Serilog;
using Umbraco.Cms.Infrastructure.Logging.Serilog.Sinks;

namespace Umbraco.Extensions
{
    public static class LoggerConfigExtensions
    {
        /// <summary>
        /// This configures Serilog with some defaults
        /// Such as adding ProcessID, Thread, AppDomain etc
        /// It is highly recommended that you keep/use this default in your own logging config customizations
        /// </summary>
        public static LoggerConfiguration MinimalConfiguration(
            this LoggerConfiguration logConfig,
            IServiceProvider serviceProvider,
            IHostEnvironment hostEnvironment,
            ILoggingConfiguration loggingConfiguration)
        {
            Serilog.Debugging.SelfLog.Enable(msg => System.Diagnostics.Debug.WriteLine(msg));

            //Set this environment variable - so that it can be used in external config file
            //add key="serilog:write-to:RollingFile.pathFormat" value="%BASEDIR%\logs\log.txt" />
            Environment.SetEnvironmentVariable("BASEDIR", hostEnvironment.MapPathContentRoot("/").TrimEnd("\\"), EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("UMBLOGDIR", loggingConfiguration.LogDirectory, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("MACHINENAME", Environment.MachineName, EnvironmentVariableTarget.Process);

            logConfig.MinimumLevel.Verbose() //Set to highest level of logging (as any sinks may want to restrict it to Errors only)
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("ApplicationId", hostEnvironment.GetTemporaryApplicationId()) // Updated later by ApplicationIdEnricher
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.With<Log4NetLevelMapperEnricher>()
                .Enrich.FromLogContext(); // allows us to dynamically enrich

            logConfig.WriteTo.Sink(serviceProvider.GetRequiredService<UmbracoSink>());

            return logConfig;
        }


        /// <summary>
        /// Outputs a .txt format log at /App_Data/Logs/
        /// </summary>
        /// <param name="logConfig">A Serilog LoggerConfiguration</param>
        /// <param name="hostEnvironment"></param>
        /// <param name="loggingSettings"></param>
        /// <param name="minimumLevel">The log level you wish the JSON file to collect - default is Verbose (highest)</param>
        public static LoggerConfiguration OutputDefaultTextFile(
            this LoggerConfiguration logConfig,
            IHostEnvironment hostEnvironment,
            LoggingSettings loggingSettings,
            LogEventLevel minimumLevel = LogEventLevel.Verbose)
        {
            //Main .txt logfile - in similar format to older Log4Net output
            //Ends with ..txt as Date is inserted before file extension substring
            logConfig.WriteTo.File(
                Path.Combine(loggingSettings.GetAbsoluteLoggingPath(hostEnvironment),  $"UmbracoTraceLog.{Environment.MachineName}..txt"),
                shared: true,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: minimumLevel,
                retainedFileCountLimit: null, //Setting to null means we keep all files - default is 31 days
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss,fff} [P{ProcessId}/D{AppDomainId}/T{ThreadId}] {Log4NetLevel}  {SourceContext} - {Message:lj}{NewLine}{Exception}");

            return logConfig;
        }

        /// <summary>
        /// Outputs a CLEF format JSON log at /App_Data/Logs/
        /// </summary>
        /// <param name="logConfig">A Serilog LoggerConfiguration</param>
        /// <param name="hostEnvironment"></param>
        /// <param name="loggingSettings">The logging configuration</param>
        /// <param name="minimumLevel">The log level you wish the JSON file to collect - default is Verbose (highest)</param>
        /// <param name="retainedFileCount">The number of days to keep log files. Default is set to null which means all logs are kept</param>
        public static LoggerConfiguration OutputDefaultJsonFile(
            this LoggerConfiguration logConfig,
            IHostEnvironment hostEnvironment,
            LoggingSettings loggingSettings,
            LogEventLevel minimumLevel = LogEventLevel.Verbose,
            int? retainedFileCount = null)
        {
            // .clef format (Compact log event format, that can be imported into local SEQ & will make searching/filtering logs easier)
            // Ends with ..txt as Date is inserted before file extension substring
            logConfig.WriteTo.File(
                new CompactJsonFormatter(),
                Path.Combine(loggingSettings.GetAbsoluteLoggingPath(hostEnvironment) ,$"UmbracoTraceLog.{Environment.MachineName}..json"),
                shared: true,
                rollingInterval: RollingInterval.Day, // Create a new JSON file every day
                retainedFileCountLimit: retainedFileCount, // Setting to null means we keep all files - default is 31 days
                restrictedToMinimumLevel: minimumLevel);

            return logConfig;
        }


    }
}
