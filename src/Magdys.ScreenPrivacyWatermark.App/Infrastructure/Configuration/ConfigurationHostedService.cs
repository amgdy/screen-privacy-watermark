using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Configuration;

internal class ConfigurationHostedService(ILogger<ConfigurationHostedService> logger, IConfiguration config) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StartAsync));
        var configurationRoot = (IConfigurationRoot)config;
        var providers = configurationRoot.Providers;

        logger.LogTrace("Searching for Azure App Configuration provider.");
        var azureAppConfigurationProvider = providers.FirstOrDefault(p => p.GetType().Name == "AzureAppConfigurationProvider");

        if (azureAppConfigurationProvider is null)
        {
            logger.LogError("Azure App Configuration provider is not found.");
            Environment.Exit(0);
        }

        logger.LogTrace("Azure App Configuration provider found. Checking for keys.");
        var loadedKeys = azureAppConfigurationProvider.GetChildKeys([], null).ToList();

        if (loadedKeys.Count == 0)
        {
            logger.LogError("Azure App Configuration provider doesn't have any keys!.");
            Environment.Exit(0);
        }

        logger.LogTrace("Keys found in Azure App Configuration provider: {count}.", loadedKeys.Count);
        logger.LogTrace("Executed {method}.", nameof(StartAsync));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
