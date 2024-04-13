using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;
using Magdys.ScreenPrivacyWatermark.App.MSGraph;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;

internal class EntraIdWatermarkSource(ILogger<EntraIdWatermarkSource> logger,
    EntraIdWatermarkSourceOptions options,
    IGraphService graphService,
    ConnectivityService connectivityService) : IWatermarkSource
{
    public bool Enabled => options.Enabled;

    public bool RequiresConnectivity => true;

    private Dictionary<string, string>? _loadedData = null;

    public async ValueTask<Dictionary<string, string>> LoadAsync(bool reload = false)
    {
        logger.LogTrace("Loading Entra ID watermark data");

        if (_loadedData != null)
        {
            logger.LogTrace("Data is already loaded, returning the loaded data.");
            return _loadedData;
        }

        if (!await connectivityService.IsConnectedAsync())
        {
            logger.LogWarning("Failed to connect to Azure AD Authority URL, cannot load Entra ID watermark data.");
            throw new InternalException("Failed to connect to Azure AD Authority URL, cannot load Entra ID watermark data.");
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
                    var value = Convert.ToString(item.Value);
                    data.Add(item.Key, value!);
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
