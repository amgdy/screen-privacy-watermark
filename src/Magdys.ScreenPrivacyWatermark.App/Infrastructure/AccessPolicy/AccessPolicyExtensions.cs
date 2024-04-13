using Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Policy;
using System.Reflection;

namespace Magdys.ScreenPrivacyWatermark.App;

public static class AccessPolicyExtensions
{
    public static HostApplicationBuilder ConfigureAccessPolicies(this HostApplicationBuilder hostApplicationBuilder, ILogger? logger = null)
    {
        hostApplicationBuilder.Services.AddOptions<AccessPolicyOptions>()
            .BindConfiguration(AccessPolicyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        hostApplicationBuilder.RegisterAccessPoliciesWithOptionsDynamic(logger);

        hostApplicationBuilder.Services.AddHostedService<AccessPolicyHostedService>();
        return hostApplicationBuilder;
    }

    private static void RegisterAccessPoliciesWithOptionsDynamic(this HostApplicationBuilder hostApplicationBuilder, ILogger? logger = null)
    {
        // Register all access policies
        var assembly = Assembly.GetExecutingAssembly();


        // Register all access policyOptionsType options
        var accessPolicyOptionTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IAccessPolicyOptions)) && !t.IsAbstract);

        foreach (var type in accessPolicyOptionTypes)
        {
            var sectionName = GetPolicyOptionSectionName(type);
            var section = hostApplicationBuilder.Configuration.GetSection(sectionName);

            logger?.LogDebug("Option: {Name} Value: {@Value}", type.Name, section);

            // Use reflection to create an instance of the options type
            var optionsInstance = Activator.CreateInstance(type);

            // Bind the configuration section to the options instance
            section.Bind(optionsInstance);

            // Register the options instance with the DI container
            hostApplicationBuilder.Services.AddSingleton(type, optionsInstance!);
        }

        var accessPolicyTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IAccessPolicy)) && !t.IsAbstract);

        foreach (var type in accessPolicyTypes)
        {
            hostApplicationBuilder.Services.AddTransient(typeof(IAccessPolicy), type);
        }
    }

    private static string GetPolicyOptionSectionName(Type type)
    {
        return GetPolicyOptionSectionName(type.Name);
    }

    private static string GetPolicyOptionSectionName(string typeName)
    {
        var name = typeName.Replace("AccessPolicyOptions", string.Empty);
        var sectionName = $"Policy:{name}";

        return sectionName;
    }
}
