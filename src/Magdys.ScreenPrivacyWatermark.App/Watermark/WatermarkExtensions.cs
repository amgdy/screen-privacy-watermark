using Magdys.ScreenPrivacyWatermark.App.MSGraph;
using Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;
using System.Reflection;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark;

internal static class WatermarkExtensions
{
    public static HostApplicationBuilder ConfigureWatermark(this HostApplicationBuilder hostApplicationBuilder, ILogger? logger = null)
    {
        hostApplicationBuilder.Services.AddOptions<WatermarkOptions>()
           .BindConfiguration(WatermarkOptions.SectionName)
           .ValidateDataAnnotations()
           .ValidateOnStart();

        hostApplicationBuilder.ConfigureMSGraph();
        hostApplicationBuilder.RegisterWatermarkSourcesWithOptionsDynamic(logger);
        hostApplicationBuilder.Services.AddSingleton<WatermarkManager>();
        return hostApplicationBuilder;
    }

    private static void RegisterWatermarkSourcesWithOptionsDynamic(this HostApplicationBuilder hostApplicationBuilder, ILogger? logger = null)
    {
        var assembly = Assembly.GetExecutingAssembly();


        var accessPolicyOptionTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IWatermarkSourceOptions)) && !t.IsAbstract);

        foreach (var type in accessPolicyOptionTypes)
        {
            var sectionName = GetPolicyOptionSectionName(type);
            var section = hostApplicationBuilder.Configuration.GetSection(sectionName);

            logger?.LogDebug("Option: {name} Value: {@Value}", type.Name, section);

            // Use reflection to create an instance of the options type
            var optionsInstance = Activator.CreateInstance(type);

            // Bind the configuration section to the options instance
            section.Bind(optionsInstance);

            // Register the options instance with the DI container
            hostApplicationBuilder.Services.AddSingleton(type, optionsInstance);
        }

        var accessPolicyTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IWatermarkSource)) && !t.IsAbstract);

        foreach (var type in accessPolicyTypes)
        {
            hostApplicationBuilder.Services.AddTransient(typeof(IWatermarkSource), type);
        }
    }

    private static string GetPolicyOptionSectionName(Type type)
    {
        return GetPolicyOptionSectionName(type.Name);
    }

    private static string GetPolicyOptionSectionName(string typeName)
    {
        var name = typeName.Replace("WatermarkSourceOptions", string.Empty);
        var sectionName = $"Source:{name}";

        return sectionName;
    }
}
