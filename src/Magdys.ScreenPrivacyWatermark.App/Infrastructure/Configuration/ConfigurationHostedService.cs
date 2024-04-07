using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Configuration;

internal class ConfigurationHostedService(ILogger<ConfigurationHostedService> logger, IConfiguration config, CachingService cachingService) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {Method}.", nameof(StartAsync));
        var configurationRoot = (IConfigurationRoot)config;
        var providers = configurationRoot.Providers;

        logger.LogTrace("Searching for Azure App Configuration provider.");
        var azureAppConfigurationProvider = providers.FirstOrDefault(p => p.GetType().Name == "AzureAppConfigurationProvider");

        if (azureAppConfigurationProvider is null)
        {
            if (cachingService.CacheItem.Configurations is not null)
            {
                logger.LogWarning("Azure App Configuration provider is not found, but configurations are loaded from the cache.");
            }
            else
            {
                logger.LogError("Azure App Configuration provider is not found and no configurations are loaded from the cache.");
                Application.Exit();
            }
        }
        else
        {
            // Azure App Configuration provider is found, proceed with the remaining checks...
            logger.LogTrace("Azure App Configuration provider found. Checking for keys.");
            var loadedKeys = azureAppConfigurationProvider.GetChildKeys([], null).ToList();

            if (loadedKeys.Count == 0)
            {
                logger.LogError("Azure App Configuration provider doesn't have any keys!.");
                Application.Exit();
            }

            logger.LogTrace("Keys found in Azure App Configuration provider: {Count}.", loadedKeys.Count);
        }


        logger.LogTrace("Executed {Method}.", nameof(StartAsync));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
