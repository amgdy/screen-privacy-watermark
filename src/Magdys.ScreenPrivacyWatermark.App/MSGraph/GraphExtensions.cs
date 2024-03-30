namespace Magdys.ScreenPrivacyWatermark.App.MSGraph;

public static class GraphExtensions
{
    public static HostApplicationBuilder ConfigureMSGraph(this HostApplicationBuilder hostApplicationBuilder, ILogger? logger = null)
    {
        hostApplicationBuilder.Services.AddOptions<GraphOptions>()
                    .BindConfiguration(GraphOptions.SectionName)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

        var usePublicClient = hostApplicationBuilder.Configuration
            .GetSection(GraphOptions.SectionName)
            .GetValue<bool>(nameof(GraphOptions.UsePublicClient));

        if (usePublicClient)
        {
            logger?.LogTrace("Using public client for MSGraph.");
            hostApplicationBuilder.Services.AddSingleton<WamTokenProvider>();
            hostApplicationBuilder.Services.AddSingleton<IGraphService, PublicGraphService>();
        }
        else
        {
            logger?.LogTrace("Using confidential client for MSGraph.");
            hostApplicationBuilder.Services.AddSingleton<IGraphService, ConfidentialGraphService>();
        }

        return hostApplicationBuilder;
    }
}
