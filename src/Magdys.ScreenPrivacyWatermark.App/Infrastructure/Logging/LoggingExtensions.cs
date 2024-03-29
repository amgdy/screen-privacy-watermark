using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Logging;
using Microsoft.ApplicationInsights.NLogTarget;
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


        var appInsightsConnectionString = hostApplicationBuilder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString");

        var logLevel = GetNLogLevel(hostApplicationBuilder.Configuration, options);

        var loggingConfig = GetNLogDefaultConfig(logLevel, appInsightsConnectionString);
        hostApplicationBuilder.Logging.AddDebug();
        hostApplicationBuilder.Logging.AddConsole();
        hostApplicationBuilder.Logging.AddNLog(loggingConfig);

        if (appInsightsConnectionString != null)
        {
            hostApplicationBuilder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
            {
                options.ConnectionString = appInsightsConnectionString;

            });

            hostApplicationBuilder.Logging.AddApplicationInsights(
                configureTelemetryConfiguration: (config) =>
                config.ConnectionString = appInsightsConnectionString,
                configureApplicationInsightsLoggerOptions: (options) => { }
                );
        }
        return hostApplicationBuilder;
    }

    public static LoggingConfiguration GetNLogDefaultConfig(NLog.LogLevel logLevel, string? appInsightsConnectionString = null)
    {
        var loggingConfiguration = new LoggingConfiguration();

        var loggerName = "${callsite:includeNamespace=false}";
        var layout = $"${{longdate:universalTime=false}}|v${{assembly-version:type=File}}|${{machinename}}|${{environment-user}}|${{pad:padding=5:inner=${{processid}}}}|${{pad:padding=5:inner=${{level:uppercase=true}}}}|{loggerName}|${{message:withexception=true}}";

        var logfile = new FileTarget("logfile")
        {
            FileName = Path.Combine(Metadata.ApplicationLogsPath, $"{Metadata.ApplicationNameShort}_${{machinename}}.log"),
            Encoding = Encoding.UTF8,
            Layout = layout,
            KeepFileOpen = false,
            ArchiveEvery = FileArchivePeriod.Day,
            ArchiveNumbering = ArchiveNumberingMode.Date,
            ArchiveDateFormat = "yyyyMMdd",
            MaxArchiveFiles = 60,
        };

        var appInsightsTarget = new ApplicationInsightsTarget()
        {
            InstrumentationKey = "9eb42fac-0fe0-43e9-98a2-4f802b19bd36",
            Name = "appInsightsTarget",
            Layout = layout,
        };

        var contextProperties = new Dictionary<string, string>
        {
            { "UserName", Environment.UserName },
            { "MachineName", Environment.MachineName },
            { "Version", "${assembly-version:type=File}" },
            { "LocalIp", "${local-ip}" },
            { "Message", "${message}" },
            { "Exception", "${exception:format=tostring}" },
            { "ExceptionData", "${exceptiondata}" },
            { "all-event-properties", "${all-event-properties:includeEmptyValues=true:includeScopeProperties=true}" }
        };

        foreach (var property in contextProperties)
        {
            appInsightsTarget.ContextProperties.Add(new Microsoft.ApplicationInsights.NLogTarget.TargetPropertyWithContext(property.Key, property.Value));
        }


        var asyncTargetFile = new AsyncTargetWrapper("asyncWrapperFile", logfile);

        var asyncTargetAppInsights = new AsyncTargetWrapper("asyncWrapperAppInsights", appInsightsTarget);

        loggingConfiguration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Info, new NullTarget(), "Microsoft.*", true);
        if (appInsightsConnectionString != null)
        {
            loggingConfiguration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, asyncTargetAppInsights, "*", false);
        }

        loggingConfiguration.AddRule(logLevel, NLog.LogLevel.Fatal, asyncTargetFile, "*", true);

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
