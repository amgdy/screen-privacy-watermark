using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System.Text;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class LoggingExtensions
{
    public static HostApplicationBuilder ConfigureLogging(this HostApplicationBuilder hostApplicationBuilder, Action<LoggingOptions>? configureOptions)
    {
        var options = new LoggingOptions();
        configureOptions?.Invoke(options);
        hostApplicationBuilder.Services.AddSingleton(options);

        var logLevel = GetNLogLevel(hostApplicationBuilder.Configuration, options);


        var loggingConfig = GetNLogDefaultConfig(logLevel, options.UseShortLoggerName);

        hostApplicationBuilder.Logging.AddDebug();
        hostApplicationBuilder.Logging.AddConsole();
        hostApplicationBuilder.Logging.AddNLog(loggingConfig);

        return hostApplicationBuilder;
    }

    public static LoggingConfiguration GetNLogDefaultConfig(NLog.LogLevel logLevel, bool useShortLoggerName)
    {
        var loggingConfiguration = new LoggingConfiguration();

        var loggerName = useShortLoggerName ? "${logger:shortName=true}" : "${logger}";
        loggerName = "${callsite}";
        loggerName = "${callsite:includeNamespace=false}";
        var layout = $"${{longdate:universalTime=true}}|v${{assembly-version:type=File}}|${{machinename}}|${{environment-user}}|${{pad:padding=5:inner=${{processid}}}}|${{pad:padding=5:inner=${{level:uppercase=true}}}}|{loggerName}|${{message:withexception=true}}";

        var logfile = new FileTarget("logfile")
        {
            FileName = Path.Combine(Metadata.ApplicationLogsPath, $"{Metadata.ApplicationNameShort}_${{machinename}}.log"),
            Encoding = Encoding.UTF8,
            Layout = layout,
            KeepFileOpen = false,
            ArchiveEvery = FileArchivePeriod.Day,
            ArchiveNumbering = ArchiveNumberingMode.Date,
            ArchiveDateFormat = "yyyyMMdd",
            MaxArchiveFiles = 30,
            ConcurrentWrites = true,
        };

        var asyncTarget = new AsyncTargetWrapper("asyncWrapper", logfile);
        loggingConfiguration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Info, new NullTarget(), "Microsoft.*", true);
        loggingConfiguration.AddRule(logLevel, NLog.LogLevel.Fatal, asyncTarget, "*", true);

        return loggingConfiguration;
    }

    public static NLog.LogLevel GetNLogLevel(IConfiguration configuration, LoggingOptions loggingOptions)
    {
        var logLevelString = configuration.GetValue<string>(loggingOptions.LogLevelConfigKey);
        if (!string.IsNullOrWhiteSpace(logLevelString))
        {
            try
            {
                return NLog.LogLevel.FromString(logLevelString);
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Warn(ex, "Invalid log level, setting the default to {level}.", loggingOptions.FallbackLogLevel);
                return loggingOptions.FallbackLogLevel;
            }
        }
        return loggingOptions.FallbackLogLevel;
    }
}
