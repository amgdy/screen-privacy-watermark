using Magdys.ScreenPrivacyWatermark.App.MSGraph;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class EntraIdGroupsAccessPolicy(
    ILogger<EntraIdGroupsAccessPolicy> logger,
    EntraIdGroupsAccessPolicyOptions options,
    IGraphService graphService) : IAccessPolicy
{
    public bool Enabled => options.AllowedGroupsIdsList.Length > 0;

    public int Order => 003;

    public bool RequiresConnectivity => true;

    public async Task<bool> CheckAccessAsync()
    {
        logger.LogTrace("Executing {Method}.", nameof(CheckAccessAsync));

        if (options.AllowedGroupsIdsList.Length == 0)
        {
            logger.LogDebug("No allowed group IDs specified. Granting access.");
            return true;
        }

        var allowedGroupsIds = new HashSet<Guid>();

        foreach (var allowedGroupId in options.AllowedGroupsIdsList)
        {
            if (Guid.TryParse(allowedGroupId, out var allowedGroupIdGuid))
            {
                allowedGroupsIds.Add(allowedGroupIdGuid);
            }
        }

        if (graphService == null)
        {
            logger.LogWarning("MicrosoftGraphService is not available. Skipping policy {PolicyName}", nameof(EntraIdGroupsAccessPolicy));
            return true;
        }

        var userGroupsIds = await graphService.GetCurrentUserGroupIdsAsync();

        userGroupsIds.IntersectWith(allowedGroupsIds);

        var hasAccess = userGroupsIds.Count > 0;

        logger.LogDebug("User {HasAccess} access based on Policy {PolicyName}", hasAccess ? "granted" : "denied", nameof(EntraIdGroupsAccessPolicy));

        logger.LogTrace("Executed {Method}.", nameof(CheckAccessAsync));

        return hasAccess;
    }
}
