using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Extensions;
using Magdys.ScreenPrivacyWatermark.App.Watermark.Options;
using Magdys.ScreenPrivacyWatermark.App.Watermark.Sources;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using System.Text;

namespace Magdys.ScreenPrivacyWatermark.App.Watermark;

public class WatermarkContext(ILogger<WatermarkContext> logger, IServiceProvider serviceProvider, ConnectivityService connectivity, IOptions<WatermarkLayoutOptions> watermarkLayoutOptions, CachingService cachingService)
{
    public Dictionary<string, string> Data { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly IWatermarkSource[] _sources = serviceProvider
        .GetServices<IWatermarkSource>()
        .Where(s => s.Enabled)
        .ToArray();

    private string? _watermarkText;
    private bool _isLoaded;

    public async ValueTask<bool> TryLoadDataAsync()
    {
        try
        {

            logger.LogTrace("Executing {Method}.", nameof(TryLoadDataAsync));
            var isConnected = await connectivity.IsConnectedAsync();

            var sources = _sources;
            if (!isConnected)
            {
                sources = _sources.Where(s => !s.RequiresConnectivity).ToArray();
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

            foreach (var source in sources)
            {
                try
                {
                    logger.LogTrace("Loading data from source: {Source}.", source.GetType().Name);
                    await resiliencePipeline.ExecuteAsync(async context =>
                    {
                        var data = await source.LoadAsync();
                        if (data is not null)
                        {
                            foreach (var (key, value) in data)
                            {
                                Data[key] = value;
                            }
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

            _isLoaded = true;

            return true;
        }


        finally
        {
            logger.LogTrace("Executed {Method}.", nameof(TryLoadDataAsync));
        }
    }

    public async ValueTask<string> GetWatermarkText()
    {
        try
        {
            logger.LogTrace("Executing {Method}.", nameof(GetWatermarkText));

            if (_watermarkText != null)
            {
                logger.LogDebug("Watermark text already loaded: {WatermarkText}", _watermarkText);
                return _watermarkText;
            }

            if (!_isLoaded)
            {
                logger.LogDebug("Data not loaded yet. Loading data...");
                await TryLoadDataAsync();
            }

            var isConnected = await connectivity.IsConnectedAsync();
            logger.LogDebug("Connectivity status: {IsConnected}", isConnected);

            var watermarkText = "";

            if (isConnected)
            {
                var connectedTokens = ExtractTokens(watermarkLayoutOptions.Value.ConnectedPattern);
                var watermarkTextBuilder = new StringBuilder(watermarkLayoutOptions.Value.ConnectedPattern);

                foreach (var token in connectedTokens)
                {
                    Data.TryGetValue(token, out var tokenValue);
                    watermarkTextBuilder.Replace($"{{{token}}}", $"{tokenValue}");
                }

                watermarkText = watermarkTextBuilder.ToString();
                cachingService.CacheWatermarkText(watermarkText);
                logger.LogDebug("Watermark text generated and cached: {WatermarkText}", watermarkText);
            }
            else
            {
                if (watermarkLayoutOptions.Value.EnableWatermarkTextCache && cachingService.CacheItem.WatermarkText != null)
                {
                    _watermarkText = cachingService.CacheItem.WatermarkText;
                    logger.LogDebug("Watermark text loaded from cache: {WatermarkText}", _watermarkText);
                    return _watermarkText;
                }

                var disconnectedTokens = ExtractTokens(watermarkLayoutOptions.Value.DisconnectedPattern);
                var watermarkTextBuilder = new StringBuilder(watermarkLayoutOptions.Value.DisconnectedPattern);

                foreach (var token in disconnectedTokens)
                {
                    Data.TryGetValue(token, out var tokenValue);
                    watermarkTextBuilder.Replace($"{{{token}}}", $"{tokenValue}");
                }

                watermarkText = watermarkTextBuilder.ToString();
                logger.LogDebug("Watermark text generated without connectivity: {WatermarkText}", watermarkText);
            }

            _watermarkText = watermarkText;


            return watermarkText;
        }
        finally
        {
            logger.LogTrace("Executed  {Method}.", nameof(GetWatermarkText));

        }
    }

    private static List<string> ExtractTokens(string template)
    {
        var tokens = new List<string>();

        tokens.AddRange(GeneratedRegexes
            .TokensExtraction()
            .Matches(template)
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value));

        return tokens;
    }
}
