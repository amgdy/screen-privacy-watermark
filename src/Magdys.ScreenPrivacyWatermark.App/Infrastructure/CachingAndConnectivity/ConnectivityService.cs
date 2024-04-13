using Polly;
using Polly.Timeout;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;

public class ConnectivityService(ILogger<ConnectivityService> logger)
{
    private readonly HttpClient _client = new();

#pragma warning disable S1075 // URIs should not be hardcoded
    private const string _azureAdAuthorityUrl = "https://login.microsoftonline.com";
#pragma warning restore S1075 // URIs should not be hardcoded

    public async Task<bool> IsConnectedAsync()
    {
        logger.LogTrace("Executing {Method}.", nameof(IsConnectedAsync));
        logger.LogTrace("Checking connection to Azure AD Authority URL: {Url}", _azureAdAuthorityUrl);

        try
        {
            var connected = false;

            TimeoutStrategyOptions timeoutStrategyOptions = new()
            {
                Timeout = TimeSpan.FromSeconds(5),
                OnTimeout = args =>
                {
                    logger.LogTrace("{OperationKey}: Execution timed out after {TotalSeconds} seconds.", args.Context.OperationKey, args.Timeout.TotalSeconds);
                    logger.LogWarning("Failed to connect to Azure AD Authority URL: {Url}", _azureAdAuthorityUrl);
                    connected = false;
                    return ValueTask.CompletedTask;
                }
            };

            var resiliencePipeline = new ResiliencePipelineBuilder()
                .AddTimeout(timeoutStrategyOptions)
                .Build();

            await resiliencePipeline.ExecuteAsync(async cancellationToken =>
            {
                var response = await _client.GetAsync(_azureAdAuthorityUrl, cancellationToken);

                // If the status code is OK, the website is available
                if (response.IsSuccessStatusCode)
                {
                    logger.LogTrace("Successfully connected to Azure AD Authority URL: {Url}", _azureAdAuthorityUrl);
                    connected = true;
                }
                else
                {
                    logger.LogWarning("Failed to connect to Azure AD Authority URL: {Url}", _azureAdAuthorityUrl);
                }
            });

            return connected;
        }
        catch (TimeoutRejectedException)
        {
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            logger.LogError("A timeout error occurred while attempting to establish a connection to the Azure AD Authority URL: {Url}. It appears you may be offline.", _azureAdAuthorityUrl);
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while trying to connect to Azure AD Authority URL: {Url}", _azureAdAuthorityUrl);
            return false;
        }
        finally
        {
            logger.LogTrace("Executed {Method}.", nameof(IsConnectedAsync));
        }
    }
}
