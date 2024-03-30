using Magdys.ScreenPrivacyWatermark.App.MSGraph;
using Magdys.ScreenPrivacyWatermark.App.Settings;
using Polly;
using Polly.Retry;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

internal class EntraIdWatermarkSource(ILogger<EntraIdWatermarkSource> logger, EntraIdWatermarkSourceOptions options, IGraphService graphService) : IWatermarkSource
{
    public bool Enabled => options.Enabled;

    public async ValueTask<bool> IsConnectedAsync()
    {
        logger.LogTrace("Checking if connected to the internet and to Entra ID service online...");
        using var httpClient = new HttpClient();

        try
        {
            var retryStrategyOptions = new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(),
                MaxRetryAttempts = 3,
                OnRetry = args =>
                {
                    logger.LogDebug("OnRetry, Attempt: {AttemptNumber}", args.AttemptNumber);

                    // Event handlers can be asynchronous; here, we return an empty ValueTask.
                    return default;
                }
            };

            var resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(retryStrategyOptions)
            .AddTimeout(TimeSpan.FromSeconds(3))
            .Build();

            var connected = false;

            await resiliencePipeline.ExecuteAsync(async context =>
            {
                var response = await httpClient.GetAsync(EntraIdSettings.AuthorityBase, context);

                // If the status code is OK, the website is available
                if (response.IsSuccessStatusCode)
                {
                    connected = true;
                }
            });

            return connected;
        }
        catch (Exception ex)
        {

            logger.LogCritical(ex, "Failed to check if you are connected to Entra ID or not.");
        }

        logger.LogTrace("Checked if connected to the internet and to Entra ID service online.");
        return false;
    }

    private Dictionary<string, string>? _loadedData = null;

    public async ValueTask<Dictionary<string, string>> LoadAsync(bool reload = false)
    {
        logger.LogTrace("Loading Entra ID watermark data");

        if (_loadedData != null)
        {
            logger.LogTrace("Data is already loaded, returning the loaded data.");
            return _loadedData;
        }

        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var user = await graphService.GetCurrentUserDataAsync(options.AttributesArray);

            if (user != null)
            {
                logger.LogDebug("Loading user {User} data", user.Id);

                var entraData = user.BackingStore.Enumerate();

                foreach (var item in entraData)
                {
                    logger.LogDebug("User data loaded: {Key}", item.Key);
                    data.Add(item.Key, item.Value?.ToString());
                }

                logger.LogDebug("User data loaded successfully.");

            }
            else
            {
                logger.LogError("Failed to load user data from Entra ID, please check the Entra ID Application Configuration.");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to load user data from Entra ID.");
        }

        // Store the loaded data
        _loadedData = data;

        logger.LogTrace("Entra ID watermark data loaded");
        return data;
    }
}
