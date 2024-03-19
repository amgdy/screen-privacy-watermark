using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class ConfigurationExtensions
{
    public static HostApplicationBuilder ConfigureAppConfiguration(this HostApplicationBuilder hostApplicationBuilder, string[] args, ILogger? logger = null)
    {
        hostApplicationBuilder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddCommandLine(args)
            .AddWindowsRegistry(options =>
            {
                options.RootKey = Metadata.RegistryRootKey;
                options.RegistryHive = Microsoft.Win32.RegistryHive.LocalMachine;
                options.Required = true;
            }, logger)
            .AddAzureAppConfiguration(options =>
            {
                var connectionString = hostApplicationBuilder.Configuration.GetConnectionString("AzureAppConfiguration");

                if (connectionString != null)
                {
                    var userNameFilter = $"{nameof(Environment.UserName)}:{Environment.UserName}";
                    var machineNameFilter = $"{nameof(Environment.MachineName)}:{Environment.MachineName}";

                    // load all configs then override with machine specific then user specific

                    options.Connect(connectionString)
                    .Select(KeyFilter.Any, LabelFilter.Null)
                    .Select(KeyFilter.Any, machineNameFilter)
                    .Select(KeyFilter.Any, userNameFilter);
                }
                else
                {
                    logger?.LogError("AzureAppConfiguration connection string is not found!");
                }
            });

        return hostApplicationBuilder;
    }
}

