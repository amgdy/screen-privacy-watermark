using Magdys.ScreenPrivacyWatermark.App.Infrastructure.ProcessProtection;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class ProcessProtectionExtensions
{
    public static HostApplicationBuilder ConfigureProcessProtection(this HostApplicationBuilder hostApplicationBuilder, Action<ProcessProtectionOptions>? configureOptions)
    {
        var options = new ProcessProtectionOptions();
        configureOptions?.Invoke(options);
        hostApplicationBuilder.Services.AddSingleton(options);
        hostApplicationBuilder.Services.AddHostedService<ProcessProtectionHostedService>();
        return hostApplicationBuilder;
    }
}
