using Magdys.ScreenPrivacyWatermark.App.Settings;
using Magdys.ScreenPrivacyWatermark.App.WatermarkProviders;
using Magdys.ScreenPrivacyWatermark.App.WatermarkProviders.EntraId;
using Magdys.ScreenPrivacyWatermark.App.WatermarkProviders.Local;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class WatermarkExtensions
{
    public static HostApplicationBuilder ConfigureWatermarkProviders(this HostApplicationBuilder hostApplicationBuilder)
    {
        hostApplicationBuilder.Services.AddOptions<WatermarkFormatSettings>()
            .BindConfiguration(WatermarkFormatSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        hostApplicationBuilder.Services.AddOptions<WatermarkProviderSettings>()
            .BindConfiguration(WatermarkProviderSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var configKey = $"{WatermarkProviderSettings.SectionName}:{nameof(WatermarkProviderSettings.Name)}";
        var providerString = hostApplicationBuilder.Configuration.GetValue<WatermarkProviderSettings.WatermarkProvider>(configKey);

        switch (providerString)
        {
            case WatermarkProviderSettings.WatermarkProvider.Local:
                hostApplicationBuilder.Services.AddSingleton<IWatermarkProvider, LocalWatermarkProvider>();
                break;
            case WatermarkProviderSettings.WatermarkProvider.EntraID:
                hostApplicationBuilder.Services.AddOptions<EntraIdSettings>()
                    .BindConfiguration(EntraIdSettings.SectionName)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
                hostApplicationBuilder.Services.AddSingleton<WindowsAccountManagerTokenProvider>();
                hostApplicationBuilder.Services.AddSingleton<MicrosoftGraphService>();
                hostApplicationBuilder.Services.AddSingleton<IWatermarkProvider, EntraIDWatermarkProvider>();
                break;
            case WatermarkProviderSettings.WatermarkProvider.ActiveDirectory:
                throw new NotSupportedException("ActiveDirectory watermark provider is not supported.");
            default:
                hostApplicationBuilder.Services.AddSingleton<IWatermarkProvider, LocalWatermarkProvider>();
                break;
        }

        hostApplicationBuilder.Services.AddSingleton<WatermarkContext>();
        return hostApplicationBuilder;
    }
}
