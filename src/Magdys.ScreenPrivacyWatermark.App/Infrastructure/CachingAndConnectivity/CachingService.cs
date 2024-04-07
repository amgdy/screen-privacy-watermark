using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;

public class CachingService(CachingOptions cachingOptions)
{
    private readonly string _cacheFilePath = Path.Combine(Metadata.ApplicationDataPath, $"{Metadata.ApplicationNameShort}.app.cache");

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    private CacheItem _cacheItem = new();

    public CacheItem CacheItem => ReadCache();

    public bool IsCacheExists()
    {
        return File.Exists(_cacheFilePath);
    }

    public void InitializeCache()
    {
        cachingOptions.Logger?.LogTrace("Executing {Method}.", nameof(InitializeCache));

        if (!IsCacheExists())
        {
            _cacheItem = new CacheItem();
            WriteCache();
        }

        cachingOptions.Logger?.LogTrace("Executed {Method}.", nameof(InitializeCache));
    }

    public void CacheConfiguration(Dictionary<string, string> configurations)
    {
        cachingOptions.Logger?.LogTrace("Executing {Method}.", nameof(CacheConfiguration));
        try
        {
            ReadCache();
            _cacheItem.Configurations = configurations;
            WriteCache();
            cachingOptions.Logger?.LogTrace("Configuration cached successfully.");
        }
        catch (Exception ex)
        {
            cachingOptions.Logger?.LogError(ex, "Failed to cache configuration.");
            throw new UiException("Failed to cache configuration. Please check the application's write permissions.");
        }
        finally
        {
            cachingOptions.Logger?.LogTrace("Executed {Method}.", nameof(CacheConfiguration));
        }
    }

    public void CacheWatermarkText(string watermarkText)
    {
        cachingOptions.Logger?.LogTrace("Executing {Method}.", nameof(CacheWatermarkText));
        try
        {
            ReadCache();
            _cacheItem.WatermarkText = watermarkText;
            WriteCache();
            cachingOptions.Logger?.LogTrace("Watermark text cached successfully.");
        }
        catch (Exception ex)
        {
            cachingOptions.Logger?.LogError(ex, "Failed to cache watermark text.");
            throw new UiException("Failed to cache watermark text. Please check the application's write permissions.");
        }
        finally
        {
            cachingOptions.Logger?.LogTrace("Executed {Method}.", nameof(CacheWatermarkText));
        }
    }

    private void WriteCache()
    {
        cachingOptions.Logger?.LogTrace("Executing {Method}.", nameof(WriteCache));
        try
        {
            var cacheJson = JsonSerializer.Serialize(_cacheItem, _jsonSerializerOptions);
            var cacheDataBytes = Encoding.UTF8.GetBytes(cacheJson);

            if (cachingOptions.EnableEncryption)
            {
                var encyptedBytes = ProtectedData.Protect(cacheDataBytes, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_cacheFilePath, encyptedBytes);
                cachingOptions.Logger?.LogTrace("Configurations cached with encryption.");
            }
            else
            {
                File.WriteAllBytes(_cacheFilePath, cacheDataBytes);
                cachingOptions.Logger?.LogTrace("Configurations cached without encryption.");
            }

            cachingOptions.Logger?.LogTrace("Cache initialized successfully.");
        }
        catch (Exception ex)
        {
            cachingOptions.Logger?.LogError(ex, "Failed to initialize cache.");
            throw new UiException("Failed to initialize cache. Please check the application's write permissions.");
        }
        finally
        {
            cachingOptions.Logger?.LogTrace("Executed {Method}.", nameof(WriteCache));
        }
    }

    private CacheItem ReadCache()
    {
        cachingOptions.Logger?.LogTrace("Executing {Method}.", nameof(ReadCache));
        try
        {
            // initialize cache if it doesn't exist
            if (!IsCacheExists())
            {
                InitializeCache();
                cachingOptions.Logger?.LogTrace("Cache initialized.");
            }

            var cacheDataBytes = File.ReadAllBytes(_cacheFilePath);
            if (cachingOptions.EnableEncryption)
            {
                cacheDataBytes = ProtectedData.Unprotect(cacheDataBytes, null, DataProtectionScope.CurrentUser);
                cachingOptions.Logger?.LogTrace("Cache read with encryption.");
            }

            var cacheJson = Encoding.UTF8.GetString(cacheDataBytes);
            _cacheItem = JsonSerializer.Deserialize<CacheItem>(cacheJson, _jsonSerializerOptions) ?? new CacheItem();

            cachingOptions.Logger?.LogTrace("Cache read successfully.");
            return _cacheItem;
        }
        catch (Exception ex)
        {
            cachingOptions.Logger?.LogError(ex, "Failed to read cache.");
            throw new UiException("Failed to read cache. Please check the application's read permissions.");
        }
        finally
        {
            cachingOptions.Logger?.LogTrace("Executed {Method}.", nameof(ReadCache));
        }

    }

}
