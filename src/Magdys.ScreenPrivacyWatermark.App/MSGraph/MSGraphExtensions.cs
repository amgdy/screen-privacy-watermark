namespace Magdys.ScreenPrivacyWatermark.App.MSGraph;

public static class MSGraphExtensions
{
    public static HostApplicationBuilder ConfigureMSGraph(this HostApplicationBuilder hostApplicationBuilder, ILogger? logger = null)
    {

        hostApplicationBuilder.Services.AddOptions<MSGraphOptions>()
                    .BindConfiguration(MSGraphOptions.SectionName)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

        hostApplicationBuilder.Services.AddSingleton<MSGraphTokenProvider>();
        hostApplicationBuilder.Services.AddSingleton<MSGraphService>();

        return hostApplicationBuilder;
    }
}
