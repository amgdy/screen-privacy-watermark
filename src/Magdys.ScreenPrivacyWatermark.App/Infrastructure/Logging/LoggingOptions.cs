namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Logging;

internal class LoggingOptions
{
    public string LogLevelConfigKey { get; set; } = "app:LogLevel";

    public NLog.LogLevel FallbackLogLevel { get; set; } = NLog.LogLevel.Info;

    public bool UseShortLoggerName { get; set; } = true;
}
