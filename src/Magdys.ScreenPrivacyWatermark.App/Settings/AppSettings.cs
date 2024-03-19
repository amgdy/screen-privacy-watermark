namespace Magdys.ScreenPrivacyWatermark.App.Settings;

internal class AppSettings
{
    public const string SectionName = "App";

    public LogLevel LogLevel { get; set; }

    public required string AzureAppConfigurationConnectionString { get; set; }

}

internal enum LogLevel
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}
