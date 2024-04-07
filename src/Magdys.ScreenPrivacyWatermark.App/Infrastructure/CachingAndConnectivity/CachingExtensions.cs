namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;

internal static class CachingExtensions
{
    public static HostApplicationBuilder ConfigureCachingAndConnectivity(this HostApplicationBuilder hostApplicationBuilder, Action<CachingOptions>? configureOptions = null)
    {
        CachingOptions cachingOptions = new();
        configureOptions?.Invoke(cachingOptions);

        hostApplicationBuilder.Services.AddSingleton<ConnectivityService>();
        hostApplicationBuilder.Services.AddSingleton(cachingOptions);
        hostApplicationBuilder.Services.AddSingleton<CachingService>();

        return hostApplicationBuilder;
    }
}
