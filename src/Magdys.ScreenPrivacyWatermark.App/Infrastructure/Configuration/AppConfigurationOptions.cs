using System.ComponentModel.DataAnnotations;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Configuration;

internal class AppConfigurationOptions
{
    public string[] Arguments { get; set; } = [];

    public ILogger? Logger { get; set; } = null;

    public bool EnableCommandLineConfiguration { get; set; } = true;

    public bool EnableWindowsRegistryConfiguration { get; set; } = true;

    public bool EnableAzureAppConfiguration { get; set; } = true;

    public string AzureAppConfigCSKey { get; set; } = "AACCS";
}
