using Magdys.ScreenPrivacyWatermark.App.Settings;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class SettingsExtensions
{
    public static HostApplicationBuilder ConfigureSettings(this HostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.Services.AddOptions<AppSettings>().BindConfiguration(AppSettings.SectionName).ValidateDataAnnotations().ValidateOnStart();
        return hostApplicationBuilder;
    }
}
