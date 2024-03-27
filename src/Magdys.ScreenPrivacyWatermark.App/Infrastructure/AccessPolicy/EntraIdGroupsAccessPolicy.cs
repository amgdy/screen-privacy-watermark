using Magdys.ScreenPrivacyWatermark.App.MSGraph;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class EntraIdGroupsAccessPolicy(
    ILogger<EntraIdGroupsAccessPolicy> logger,
    EntraIdGroupsAccessPolicyOptions options,
    IServiceProvider serviceProvider) : IAccessPolicy
{
    public bool Enabled => options.AllowedGroupsIdsList.Length > 0;

    public int Order => 003;

    public async Task<bool> CheckAccessAsync()
    {
        logger.LogTrace("Executing {method}.", nameof(CheckAccessAsync));

        if (options.AllowedGroupsIdsList.Length == 0)
        {
            logger.LogDebug("No allowed group IDs specified. Granting access.");
            return true;
        }

        var microsoftGraphService = serviceProvider.GetService<MSGraphService>();

        if (microsoftGraphService == null)
        {
            logger.LogWarning("MicrosoftGraphService is not available. Skipping policy {PolicyName}", nameof(EntraIdGroupsAccessPolicy));
            return true;
        }

        var client = microsoftGraphService.Client;

        var userGroupsIds = new List<string>();

        var pageIterator = PageIterator<DirectoryObject, DirectoryObjectCollectionResponse>.CreatePageIterator(
            client: client,
            page: await client.Me.MemberOf.GetAsync(),
            callback: (directoryObject) =>
            {
                userGroupsIds.Add(directoryObject.Id);
                return true;
            });

        await pageIterator.IterateAsync();

        var commonGroupIds = options.AllowedGroupsIdsList.Intersect(userGroupsIds);

        var hasAccess = commonGroupIds.Any();

        logger.LogDebug("User {hasAccess} access based on Policy {PolicyName}", hasAccess ? "granted" : "denied", nameof(EntraIdGroupsAccessPolicy));

        logger.LogTrace("Executed {method}.", nameof(CheckAccessAsync));

        return hasAccess;
    }
}
