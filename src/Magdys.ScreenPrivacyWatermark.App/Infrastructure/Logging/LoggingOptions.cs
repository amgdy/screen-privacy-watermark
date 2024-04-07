namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Logging;

internal class LoggingOptions
{
    public string LogLevelConfigKey { get; set; } = "LogLevel";

    public NLog.LogLevel FallbackLogLevel { get; set; } = NLog.LogLevel.Info;
}
