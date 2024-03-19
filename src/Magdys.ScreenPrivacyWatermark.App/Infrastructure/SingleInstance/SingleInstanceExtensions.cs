using Magdys.ScreenPrivacyWatermark.App.Infrastructure.SingleInstance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class SingleInstanceExtensions
{
    public static HostApplicationBuilder ConfigureSingleInstance(this HostApplicationBuilder hostApplicationBuilder, Action<SingleInstanceOptions>? configureOptions)
    {
        var options = new SingleInstanceOptions();
        configureOptions?.Invoke(options);
        hostApplicationBuilder.Services.AddSingleton(options);
        hostApplicationBuilder.Services.AddHostedService<SingleInstanceHostedService>();
        return hostApplicationBuilder;
    }
}
