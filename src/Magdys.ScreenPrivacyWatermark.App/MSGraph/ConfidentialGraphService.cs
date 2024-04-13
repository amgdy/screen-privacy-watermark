using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using System.Security.Principal;

namespace Magdys.ScreenPrivacyWatermark.App.MSGraph;

public class ConfidentialGraphService(ILogger<ConfidentialGraphService> logger, IOptions<GraphOptions> graphOptions) : IGraphService
{
    private Guid? _userId = null;
    private GraphServiceClient? _client;

    public GraphServiceClient Client => _client ??= GetClient(logger, graphOptions.Value);

    private static GraphServiceClient GetClient(ILogger logger, GraphOptions options)
    {
        logger.LogTrace("Executing {Method}.", nameof(GetClient));
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        logger.LogDebug("Creating Graph client with client secret.");
        var clientCertificateCredentialOptions = new ClientCertificateCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };

        var slientSecretCredential = new ClientSecretCredential(options.TenantId.ToString(), options.ClientId.ToString(), options.ClientSecret, clientCertificateCredentialOptions);

        var scopes = new[] { "https://graph.microsoft.com/.default" };

        var client = new GraphServiceClient(slientSecretCredential, scopes);

        logger.LogDebug("Graph client created.");


        logger.LogTrace("Executed {Method}.", nameof(GetClient));
        return client;
    }

    public async ValueTask<Guid> GetCurrentUserIdAsync()
    {
        logger.LogTrace("Executing {Method}.", nameof(GetCurrentUserIdAsync));

        if (_userId.HasValue)
        {
            logger.LogTrace("User ID already retrieved: {UserId}", _userId);
            return _userId.Value;
        }

        var windowsIdentity = WindowsIdentity.GetCurrent();
        var userSid = windowsIdentity.User?.Value;

        logger.LogDebug("Current user SID: {UserSid}", userSid);

        var client = Client;

        var users = await client.Users.GetAsync(r =>
        {
            r.QueryParameters.Filter = $"securityIdentifier eq '{userSid}'";
            r.QueryParameters.Select = ["Id"];
            r.QueryParameters.Top = 1;
        });

        var user = users?.Value?.FirstOrDefault() ?? throw new InvalidOperationException("User not found.");
        if (Guid.TryParse(user.Id, out var userId))
        {
            logger.LogDebug("Current user ID: {UserId}", userId);
            _userId = userId;
        }
        else
        {
            throw new InvalidOperationException("User ID is not a valid GUID.");
        }

        logger.LogTrace("Executed {Method}.", nameof(GetCurrentUserIdAsync));

        return userId;
    }

    public async ValueTask<User> GetCurrentUserDataAsync(params string[] properties)
    {
        logger.LogTrace("Executing {Method}.", nameof(GetCurrentUserDataAsync));
        ArgumentNullException.ThrowIfNull(properties);

        logger.LogDebug("Getting current user with properties: {@Properties}", properties);
        try
        {
            var client = Client;

            var userId = _userId ??= await GetCurrentUserIdAsync();

            string[] defaultProperties = ["id", "displayName", "mail", "userPrincipalName", "securityIdentifier"];

            properties = properties.Length == 0 ? defaultProperties : properties;

            logger.LogDebug("Requested user properties: {@Properties}", properties);

            var user = await client.Users[userId.ToString()].GetAsync(r =>
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

        throw new InternalException("Failed to get current user data.");
    }

    public async ValueTask<HashSet<Guid>> GetCurrentUserGroupIdsAsync()
    {
        logger.LogTrace("Executing {Method}.", nameof(GetCurrentUserGroupIdsAsync));
        var userGroupsIds = new HashSet<Guid>();

        try
        {
            var userId = _userId ??= await GetCurrentUserIdAsync();
            var client = Client;

            logger.LogDebug("Current user ID: {UserId}", userId);

#pragma warning disable CS8604 // Possible null reference argument.
            var pageIterator = PageIterator<DirectoryObject, DirectoryObjectCollectionResponse>.CreatePageIterator(
                client: client,
                page: await client.Users[userId.ToString()].MemberOf.GetAsync(),
                callback: (directoryObject) =>
                {
                    if (Guid.TryParse(directoryObject.Id, out var groupId))
                    {
                        userGroupsIds.Add(groupId);
                    }

                    return true;
                });
#pragma warning restore CS8604 // Possible null reference argument.

            await pageIterator.IterateAsync();

            logger.LogDebug("User is member of {NumberOfGroups} groups", userGroupsIds.Count);
        }
        catch (ODataError odataError)
        {
            logger.LogCritical(odataError, "OData Error {@Code}", odataError.Error);        }

        logger.LogTrace("Executed {Method}.", nameof(GetCurrentUserGroupIdsAsync));
        return userGroupsIds;
    }
}
