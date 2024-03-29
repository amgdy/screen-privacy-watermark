using Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;
using Microsoft.Extensions.Options;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.Policy;

internal class AccessPolicyHostedService(ILogger<AccessPolicyHostedService> logger, IOptions<AccessPolicyOptions> options, IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StartAsync));

        var hasAccess = await EvaulateAccessPolicies();

        if (!hasAccess)
        {
            logger.LogTrace("Access denied. Exiting application.");
            Environment.Exit(0);
        }

        logger.LogTrace("Access granted.");
        logger.LogTrace("Executed {method}.", nameof(StartAsync));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("Executing {method}.", nameof(StopAsync));
        logger.LogTrace("Executed {method}.", nameof(StopAsync));
        return Task.CompletedTask;
    }

    private async Task<bool> EvaulateAccessPolicies()
    {
        logger.LogTrace("Executing {method}.", nameof(EvaulateAccessPolicies));

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

        switch (evaluationMode)
        {
            case EvaluationMode.Any:
                foreach (var policy in policies)
                {
                    logger.LogDebug("Policy {PolicyName} is enabled and has order {Order}", policy.GetType().Name, policy.Order);

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
