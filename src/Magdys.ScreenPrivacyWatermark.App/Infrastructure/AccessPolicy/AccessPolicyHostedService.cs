using Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;
using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Caching;
using Microsoft.Extensions.Options;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Policy;

internal class AccessPolicyHostedService(ILogger<AccessPolicyHostedService> logger, IOptions<AccessPolicyOptions> options, IServiceProvider serviceProvider, ConnectivityService connectivityService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {Method}.", nameof(StartAsync));

        var hasAccess = await EvaulateAccessPolicies();

        if (!hasAccess)
        {
            logger.LogTrace("Access denied. Exiting application.");
            Application.Exit();
        }

        logger.LogTrace("Access granted.");
        logger.LogTrace("Executed {Method}.", nameof(StartAsync));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {Method}.", nameof(StopAsync));
        logger.LogTrace("Executed {Method}.", nameof(StopAsync));
        return Task.CompletedTask;
    }

    private async Task<bool> EvaulateAccessPolicies()
    {
        logger.LogTrace("Executing {Method}.", nameof(EvaulateAccessPolicies));

        var evaluationMode = options.Value.EvaluationMode;
        logger.LogDebug("Evaluation mode: {EvaluationMode}", evaluationMode);

        var policies = serviceProvider
           .GetServices<IAccessPolicy>()
           .Where(p => p.Enabled)
           .OrderBy(p => p.Order)
           .ToList();

        // if there is no policy enabled, grant access
        if (policies.Count == 0)
        {
            logger.LogDebug("No enabled policies found. Granting access.");
            return true;
        }

        var isConnected = await connectivityService.IsConnectedAsync();

        switch (evaluationMode)
        {
            case EvaluationMode.Any:
                foreach (var policy in policies)
                {
                    logger.LogDebug("Policy {PolicyName} is enabled and has order {Order}", policy.GetType().Name, policy.Order);

                    if (policy.RequiresConnectivity && !isConnected)
                    {
                        logger.LogWarning("Validation of policy {PolicyName} has been skipped due to its requirement for internet connectivity and the current offline status of the system.", policy.GetType().Name);

                        continue;
                    }

                    var hasAccess = await policy.CheckAccessAsync();
                    if (hasAccess)
                    {
                        logger.LogInformation("User granted access based on Policy {PolicyName}", policy.GetType().Name);
                        return true;
                    }
                }

                return false;

            case EvaluationMode.All:
                foreach (var policy in policies)
                {
                    logger.LogDebug("Policy {PolicyName} is enabled and has order {Order}", policy.GetType().Name, policy.Order);

                    if (policy.RequiresConnectivity && !isConnected)
                    {
                        logger.LogWarning("Validation of policy {PolicyName} has been skipped due to its requirement for internet connectivity and the current offline status of the system.", policy.GetType().Name);

                        continue;
                    }

                    var hasAccess = await policy.CheckAccessAsync();
                    if (!hasAccess)
                    {
                        logger.LogInformation("User denied access based on Policy {PolicyName}", policy.GetType().Name);
                        return false;
                    }
                }

                return true;

            default:
                throw new InvalidOperationException($"Unsupported evaluation mode: {evaluationMode}");
        }
    }
}
