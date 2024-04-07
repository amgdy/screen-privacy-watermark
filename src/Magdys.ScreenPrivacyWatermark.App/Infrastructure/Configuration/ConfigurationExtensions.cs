using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Magdys.ScreenPrivacyWatermark.App;

internal static class ConfigurationExtensions
{
    public static HostApplicationBuilder ConfigureAppConfiguration(this HostApplicationBuilder hostApplicationBuilder, Action<AppConfigurationOptions>? configureOptions = null)
    {
        AppConfigurationOptions configOptions = new();
        configureOptions?.Invoke(configOptions);

        var serviceProvider = hostApplicationBuilder.Services.BuildServiceProvider();

        var cachingService = serviceProvider.GetRequiredService<CachingService>();
        var connectivityService = serviceProvider.GetRequiredService<ConnectivityService>();

        hostApplicationBuilder.Configuration.SetBasePath(Directory.GetCurrentDirectory());

        if (configOptions.EnableCommandLineConfiguration)
        {
            hostApplicationBuilder.Configuration.AddCommandLine(configOptions.Arguments);
        }

        if (configOptions.EnableWindowsRegistryConfiguration)
        {
            hostApplicationBuilder.Configuration.AddWindowsRegistry(options =>
            {
                options.RootKey = Metadata.RegistryRootKey;
                options.RegistryHive = Microsoft.Win32.RegistryHive.LocalMachine;
                options.Required = true;
                options.Depth = 1;
            }, configOptions.Logger);
        }

        if (configOptions.EnableAzureAppConfiguration)
        {
            ConfigureAzureAppConfiguration(hostApplicationBuilder, configOptions, cachingService, connectivityService);
        }

        hostApplicationBuilder.Services.AddHostedService<ConfigurationHostedService>();

        return hostApplicationBuilder;
    }

    private static void ConfigureAzureAppConfiguration(HostApplicationBuilder hostApplicationBuilder,
        AppConfigurationOptions configOptions,
        CachingService cachingService,
        ConnectivityService connectivityService)
    {
        configOptions.Logger?.LogTrace("Executing {Method}.", nameof(ConfigureAzureAppConfiguration));
        try
        {
            var isConnected = connectivityService.IsConnectedAsync().GetAwaiter().GetResult();
            configOptions.Logger?.LogDebug("Internet connection status: {IsConnected}", isConnected);

            if (isConnected)
            {
                var connectionString = hostApplicationBuilder.Configuration.GetValue<string>(configOptions.AzureAppConfigCSKey);
                configOptions.Logger?.LogDebug("Azure App Configuration connection string: {ConnectionString}", connectionString);

                if (connectionString != null)
                {
                    hostApplicationBuilder.Configuration.AddAzureAppConfiguration(options =>
                    {
                        options.ConfigureClientOptions(cco =>
                        {
                            cco.Retry.MaxDelay = TimeSpan.FromSeconds(10);
                        });

                        // Get Azure App Configuration connection string
                        var userNameFilter = $"{nameof(Environment.UserName)}:{Environment.UserName}";
                        var machineNameFilter = $"{nameof(Environment.MachineName)}:{Environment.MachineName}";

                        // load all configs then override with machine specific then user specific

                        options.Connect(connectionString)
                        .Select(KeyFilter.Any, LabelFilter.Null)
                        .Select(KeyFilter.Any, machineNameFilter)
                        .Select(KeyFilter.Any, userNameFilter);

                    }, optional: false);

                    configOptions.Logger?.LogDebug("Azure App Configuration connected successfully.");

                    // cache the configuration to be used in case of offline mode
                    var configData = hostApplicationBuilder.Configuration.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    cachingService.CacheConfiguration(configData!);

                    configOptions.Logger?.LogDebug("Configurations cached successfully.");
                }
                else
                {
                    throw new UiException("Please ensure Azure App Configuration is correctly configured.");
                }
            }
            else
            {
                if (cachingService.CacheItem.Configurations is null)
                {
                    throw new UiException("Please ensure you're connected to the internet and have the application configured properly.");
                }
                else
                {
                    configOptions.Logger?.LogInformation("Using cached configurations.");
                    hostApplicationBuilder.Configuration.AddInMemoryCollection(cachingService.CacheItem.Configurations!);
                }
            }
        }
        catch (TimeoutException ex)
        {
            configOptions.Logger?.LogDebug(ex, "TimeoutException occurred while connecting to Azure App Configuration.");

            if (cachingService.IsCacheExists() && cachingService.CacheItem.Configurations != null)
            {
                hostApplicationBuilder.Configuration.AddInMemoryCollection(cachingService.CacheItem.Configurations!);
                configOptions.Logger?.LogInformation("Using cached configurations due to TimeoutException.");
            }
            else
            {
                throw new UiException("Please ensure you have a valid internet connection.", ex);
            }
        }
        finally
        {
            configOptions.Logger?.LogTrace("Executed {Method}.", nameof(ConfigureAzureAppConfiguration));
        }
    }
}

