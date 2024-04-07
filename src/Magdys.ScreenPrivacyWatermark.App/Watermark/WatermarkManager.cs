using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;
using Magdys.ScreenPrivacyWatermark.App.Watermark.Options;
using Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark;

[Obsolete("This class is obsolete, use WatermarkContext instead.")]
public class WatermarkManager(
    ILogger<WatermarkManager> logger,
    IServiceProvider serviceProvider,
    IOptions<WatermarkLayoutOptions> watermarkOptions,
    CachingService cachingService)
{
    public Dictionary<string, string> Data { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly string _cachePath = Path.Combine(Metadata.ApplicationDataPath, "spw.cache.bin");

    private bool _isConnected;

    private string? _watermarkText;

    private readonly IWatermarkSource[] _sources = serviceProvider
        .GetServices<IWatermarkSource>()
        .Where(s => s.Enabled)
        .ToArray();


    /// <summary>
    /// Asynchronously checks if all sources are connected.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a boolean indicating the connection status.
    /// </returns>
    public async ValueTask<bool> IsConnectedAsync()
    {
        logger.LogTrace("Executing {Method}.", nameof(IsConnectedAsync));
        var connectionStatus = new List<bool>();

        foreach (var source in _sources)
        {
            //connectionStatus.Add(await source.IsConnectedAsync());
        }

        var isConnected = connectionStatus.TrueForAll(c => c);

        _isConnected = isConnected;

        logger.LogTrace("Executed {Method}.", nameof(IsConnectedAsync));

        return _isConnected;
    }

    public async ValueTask<bool> TryLoadAsync()
    {
        logger.LogTrace("Executing {Method}.", nameof(TryLoadAsync));
        var retryStrategyOptions = new RetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = args =>
            {
                logger.LogTrace("OnRetry, Attempt: {AttemptNumber}", args.AttemptNumber);

                return ValueTask.CompletedTask;
            }
        };

        var timeoutStrategyOptions = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(5),
            OnTimeout = args =>
            {
                logger.LogTrace("{OperationKey}: Execution timed out after {TotalSeconds} seconds.", args.Context.OperationKey, args.Timeout.TotalSeconds);
                return default;
            }
        };

        var resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(retryStrategyOptions)
            .AddTimeout(timeoutStrategyOptions)
            .Build();

        var connectionStatus = new List<bool>();

        foreach (var source in _sources)
        {
            logger.LogTrace("Checking connection for source: {Source}.", source.GetType().Name);
            try
            {
                var isConnected = await source.IsConnectedAsync();
                connectionStatus.Add(isConnected);

                if (!isConnected)
                {
                    logger.LogWarning("Failed to connect to {Source}", source.GetType().Name);
                    continue;
                }

                logger.LogTrace("Loading data from source: {Source}.", source.GetType().Name);
                await resiliencePipeline.ExecuteAsync(async context =>
                {
                    var data = await source.LoadAsync();
                    foreach (var item in data)
                    {
                        Data[item.Key] = item.Value;
                    }
                });
                logger.LogTrace("Data loaded successfully from source: {Source}.", source.GetType().Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load watermark data from {Source}", source.GetType().Name);
                return false;
            }
        }

        _isConnected = connectionStatus.TrueForAll(c => c);

        logger.LogTrace("Executed {Method}.", nameof(TryLoadAsync));
        return true;
    }

    public async ValueTask<string> GetWatermarkText()
    {
        logger.LogTrace("Executing {Method}.", nameof(GetWatermarkText));
        if (_watermarkText is not null)
        {
            return _watermarkText;
        }

        var retryStrategyOptions = new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            OnRetry = args =>
            {
                logger.LogTrace("OnRetry, Attempt: {AttemptNumber}", args.AttemptNumber);

                return ValueTask.CompletedTask;
            }
        };

        var resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(retryStrategyOptions)
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();

        await resiliencePipeline.ExecuteAsync(async context =>
               {
                   if (!await TryLoadAsync())
                   {
                       throw new InternalException("Failed to load watermark data.");
                   }
               });


        var watermarkText = "";

        if (_isConnected)
        {

            var connectedTokens = ExtractTokens(watermarkOptions.Value.ConnectedPattern);
            var watermarkTextBuilder = new StringBuilder(watermarkOptions.Value.ConnectedPattern);

            foreach (var token in connectedTokens)
            {
                Data.TryGetValue(token, out var tokenValue);
                watermarkTextBuilder.Replace($"{{{token}}}", $"{tokenValue}");
            }

            watermarkText = watermarkTextBuilder.ToString();

            if (watermarkOptions.Value.EnableWatermarkTextCache)
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
            if (watermarkOptions.Value.EnableWatermarkTextCache)
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


            var disconnectedTokens = ExtractTokens(watermarkOptions.Value.DisconnectedPattern);
            var watermarkTextBuilder = new StringBuilder(watermarkOptions.Value.DisconnectedPattern);

            foreach (var token in disconnectedTokens)
            {
                Data.TryGetValue(token, out var tokenValue);
                watermarkTextBuilder.Replace($"{{{token}}}", $"{tokenValue}");
            }

            watermarkText = watermarkTextBuilder.ToString();


        }

        _watermarkText = watermarkText;

        logger.LogTrace("Executed  {Method}.", nameof(GetWatermarkText));
        return watermarkText;
    }

    private static List<string> ExtractTokens(string template)
    {
        const string tokensRegex = "{(?<token>[A-Za-z0-9-_]{2,64})}";
        var regex = new Regex(tokensRegex, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
        var tokens = new List<string>();

        tokens.AddRange(regex.Matches(template)
                       .Where(match => match.Success)
                       .Select(match => match.Groups[1].Value));

        return tokens;
    }
}

