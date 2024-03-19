using Magdys.ScreenPrivacyWatermark.App.Infrastructure;
using Magdys.ScreenPrivacyWatermark.App.Settings;
using Magdys.ScreenPrivacyWatermark.App.WatermarkProviders.Local;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Magdys.ScreenPrivacyWatermark.App.WatermarkProviders;

public class WatermarkContext(
    ILogger<WatermarkContext> logger,
    IWatermarkProvider watermarkProvider,
    IOptions<WatermarkProviderSettings> watermarkProviderSetting,
    IOptions<WatermarkFormatSettings> watermarkFormatSetting
    )
{
    public IWatermarkProvider Provider => watermarkProvider;

    public WatermarkFormatSettings Format => watermarkFormatSetting.Value;

    private readonly string _cachePath = Path.Combine(Metadata.ApplicationDataPath, "cache.bin");

    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private string _watermarkText;

    private const string _tokensRegex = "{(?<token>[A-Za-z0-9-_]{2,64})}";

    public async Task<string> GetWatermarkText()
    {
        if (!string.IsNullOrWhiteSpace(_watermarkText))
        {
            return _watermarkText;
        }

        var dataStore = await LocalWatermarkProvider.GetLocalDataAsync(watermarkProviderSetting.Value.DataDateCultures.ToArray());

        string? watermarkText = null;

        var isOnline = await watermarkProvider.IsOnline();

        if (isOnline)
        {
            logger.LogDebug("Watermark provider is online.");
            watermarkText = watermarkProviderSetting.Value.WatermarkOnlinePattern;

            var onlineTokens = ExtractTokens(watermarkProviderSetting.Value.WatermarkOnlinePattern);

            await Extensions.ExecuteWithTimeoutAsync(
                action: async (r) => await watermarkProvider.LoadDataAsync(onlineTokens.ToArray()),
                timeout: TimeSpan.FromSeconds(10),
                retries: 2,
                retryCondition: async () => await Task.FromResult(watermarkProvider.IsLoaded),
                retryDelay: TimeSpan.FromSeconds(2),
                logger: logger,
                semaphore: _semaphore
                );


            foreach (var item in watermarkProvider.Data)
            {
                dataStore.TryAdd(item.Key, item.Value);
            }

            foreach (var token in onlineTokens)
            {
                var tokenValue = dataStore
                    .Where(x => x.Key.Equals(token, StringComparison.InvariantCultureIgnoreCase))
                    .Select(x => x.Value)
                    .FirstOrDefault();

                watermarkText = watermarkText.Replace($"{{{token}}}", $"{tokenValue}", StringComparison.InvariantCultureIgnoreCase);
            }

            if (watermarkProviderSetting.Value.EnableCache)
            {
                logger.LogDebug("Watermark caching is enabled.");
                var watermarkTextBytes = Encoding.UTF8.GetBytes(watermarkText);
                var encyptedBytes = ProtectedData.Protect(watermarkTextBytes, null, DataProtectionScope.CurrentUser);
                await File.WriteAllBytesAsync(_cachePath, encyptedBytes);
                logger.LogDebug("Watermark cached successfully.");
            }
        }
        else
        {
            logger.LogDebug("Watermark provider is offline.");
            if (watermarkProviderSetting.Value.EnableCache)
            {
                logger.LogDebug("Watermark caching is enabled.");

                if (File.Exists(_cachePath))
                {
                    var encyptedBytes = await File.ReadAllBytesAsync(_cachePath);
                    try
                    {
                        var watermarkTextBytes = ProtectedData.Unprotect(encyptedBytes, null, DataProtectionScope.CurrentUser);
                        watermarkText = Encoding.UTF8.GetString(watermarkTextBytes);
                        _watermarkText = watermarkText;
                        logger.LogDebug("Watermark cache file found and decrypted successfully.");
                        return watermarkText;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to decrypt watermark cache file.");
                    }
                }
                else
                {
                    logger.LogWarning("Watermark cache file not found.");
                }
            }

            logger.LogDebug("Loading watermark from offline pattern.");
            watermarkText = watermarkProviderSetting.Value.WatermarkOfflinePattern;

            var offlineTokens = ExtractTokens(watermarkProviderSetting.Value.WatermarkOfflinePattern);

            foreach (var token in offlineTokens)
            {
                var tokenValue = dataStore
                    .Where(x => x.Key.Equals(token, StringComparison.InvariantCultureIgnoreCase))
                    .Select(x => x.Value)
                    .FirstOrDefault();

                watermarkText = watermarkText.Replace($"{{{token}}}", $"{tokenValue}", StringComparison.InvariantCultureIgnoreCase);
            }
        }


        _watermarkText = watermarkText;

        return watermarkText;
    }

    private static List<string> ExtractTokens(string template)
    {
        var regex = new Regex(_tokensRegex, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
        var tokens = new List<string>();

        tokens.AddRange(regex.Matches(template)
                       .Where(match => match.Success)
                       .Select(match => match.Groups[1].Value));

        return tokens;
    }
}
