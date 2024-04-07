using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Magdys.ScreenPrivacyWatermark.App.MSGraph;

internal class PublicGraphService(ILogger<PublicGraphService> logger, WamTokenProvider wamTokenProvider) : IGraphService
{
    public GraphServiceClient Client { get; } = GetClient(logger, wamTokenProvider);

    private static GraphServiceClient GetClient(ILogger logger, WamTokenProvider tokenProvider)
    {
        logger.LogTrace("Executing {Method}.", nameof(GetClient));

        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(tokenProvider);

        logger.LogDebug("Creating Graph client with WAM token provider.");

        var client = new GraphServiceClient(new BaseBearerTokenAuthenticationProvider(tokenProvider));

        logger.LogTrace("Executed {Method}.", nameof(GetClient));

        return client;
    }

    public async ValueTask<User> GetCurrentUserDataAsync(params string[] properties)
    {
        logger.LogTrace("Executing {Method}.", nameof(GetCurrentUserDataAsync));
        logger.LogDebug("Getting current user with properties: {@Properties}", properties);
        try
        {
            var client = Client;

            string[] defaultProperties = ["id", "displayName", "mail", "userPrincipalName", "securityIdentifier"];

            properties = properties.Length == 0 ? defaultProperties : properties;

            logger.LogDebug("Requested user properties: {@Properties}", properties);

            var user = await client.Me.GetAsync(r =>
            {
                r.QueryParameters.Select = properties;
            }) ?? throw new InvalidOperationException("User not found.");

            logger.LogTrace("Executed {Method}.", nameof(GetCurrentUserDataAsync));
            return user;
        }
        catch (ODataError odataError)
        {
            logger.LogCritical(odataError, "OData Error {@Code}", odataError.Error);
        }

        return null;
    }

    public async ValueTask<HashSet<Guid>> GetCurrentUserGroupIdsAsync()
    {
        logger.LogTrace("Executing {Method}.", nameof(GetCurrentUserGroupIdsAsync));
        var userGroupsIds = new HashSet<Guid>();

        try
        {
            var client = Client;

            var pageIterator = PageIterator<DirectoryObject, DirectoryObjectCollectionResponse>.CreatePageIterator(
                client: client,
                page: await client.Me.MemberOf.GetAsync(),
                callback: (directoryObject) =>
                {
                    if (Guid.TryParse(directoryObject.Id, out var groupId))
                    {
                        userGroupsIds.Add(groupId);
                    }

                    return true;
                });

            await pageIterator.IterateAsync();

            logger.LogDebug("User is member of {NumberOfGroups} groups", userGroupsIds.Count);
        }
        catch (ODataError odataError)
        {
            logger.LogCritical(odataError, "OData Error {@Code}", odataError.Error);
        }

        logger.LogTrace("Executed {Method}.", nameof(GetCurrentUserGroupIdsAsync));
        return userGroupsIds;
    }
}
