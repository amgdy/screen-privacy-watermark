using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Magdys.ScreenPrivacyWatermark.App.MSGraph;
/// <summary>
/// Provides access tokens using the Windows Account Manager and Integrated Windows Authentication.
/// </summary>
/// <param name="logger">The logger used for logging events in this class.</param>
/// <param name="settings">The settings used for configuring the Microsoft Graph API.</param>
public class WamTokenProvider(ILogger<WamTokenProvider> logger, IOptions<GraphOptions> settings) : IAccessTokenProvider
{
    private readonly IPublicClientApplication _pcaWithBroker = PublicClientApplicationBuilder
        .Create(settings.Value.ClientId.ToString())
        .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
        .WithAuthority(settings.Value.Authority)
        .WithLogging((level, message, containsPii) =>
        {
            logger.LogTrace("{Level} {Message} {ContainsPii}", level, message, containsPii);
        }, logLevel: Microsoft.Identity.Client.LogLevel.Warning, enablePiiLogging: true, enableDefaultPlatformLogging: true)
        .Build();

    private readonly IPublicClientApplication _pca = PublicClientApplicationBuilder
        .Create(settings.Value.ClientId.ToString())
        .WithAuthority(settings.Value.Authority)
        .WithLogging((level, message, containsPii) => logger.LogTrace("{Level} {Message} {ContainsPii}", level, message, containsPii), Microsoft.Identity.Client.LogLevel.Warning)
        .Build();

    private readonly MsalCacheHelper _msalCacheHelper = CreateCacheHelperAsync().GetAwaiter().GetResult();

    public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();

    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        logger.LogTrace("Executing {Method}.", nameof(GetAuthorizationTokenAsync));
        logger.LogTrace("Getting accounts from the broker.");
        var accounts = await _pcaWithBroker.GetAccountsAsync();

        var account = accounts.FirstOrDefault();

        AuthenticationResult? authenticationResult = null;

        try
        {
            if (account == null)
            {
                logger.LogTrace("Account not found. Using {Account}.", PublicClientApplication.OperatingSystemAccount);

                logger.LogTrace("Acquiring token silently.");
                authenticationResult = await _pcaWithBroker
                    .AcquireTokenSilent(settings.Value.Scopes, PublicClientApplication.OperatingSystemAccount)
                    .ExecuteAsync(cancellationToken);
            }
            else
            {
                logger.LogTrace("Account found: {Account}.", account);
                logger.LogTrace("Acquiring token silently.");
                authenticationResult = await _pcaWithBroker
                    .AcquireTokenSilent(settings.Value.Scopes, account)
                    .ExecuteAsync(cancellationToken);
            }

            logger.LogTrace("Registering cache.");
            _msalCacheHelper.RegisterCache(_pcaWithBroker.UserTokenCache);

        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Failed to acquire WAM token! of type {ExceptionType}", exception.GetType());

            // Another fallback to use Integrated Windows Authentication
            logger.LogTrace("Fallback to use Integrated Windows Authentication.");

            try
            {

                authenticationResult = await _pca.AcquireTokenByIntegratedWindowsAuth(settings.Value.Scopes)
                   .ExecuteAsync(CancellationToken.None);
                logger.LogTrace("Registering cache.");
                _msalCacheHelper.RegisterCache(_pca.UserTokenCache);
            }
            catch (MsalUiRequiredException ex)
            {
                // MsalUiRequiredException: AADSTS65001: The user or administrator has not consented to use the application
                // with ID '{appId}' named '{appName}'.Send an interactive authorization request for this user and resource.

                // you need to get user consent first. This can be done, if you are not using .NET Core (which does not have any Web UI)
                // by doing (once only) an AcquireToken interactive.

                // If you are using .NET core or don't want to do an AcquireTokenInteractive, you might want to suggest the user to navigate
                // to a URL to consent: https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={clientId}&response_type=code&scope=user.read

                // AADSTS50079: The user is required to use multi-factor authentication.
                // There is no mitigation - if MFA is configured for your tenant and Azure AD decides to enforce it,
                // you need to fallback to an interactive flows such as AcquireTokenInteractive or AcquireTokenByDeviceCode

                logger.LogCritical(ex, "Error acquiring token of type {Type}.", typeof(MsalUiRequiredException));
            }
            catch (MsalServiceException ex)
            {
                // Kind of errors you could have (in ex.Message)

                // MsalServiceException: AADSTS90010: The grant type is not supported over the /common or /consumers endpoints. Please use the /organizations or tenant-specific endpoint.
                // you used common.
                // Mitigation: as explained in the message from Azure AD, the authoriy needs to be tenanted or otherwise organizations

                // MsalServiceException: AADSTS70002: The request body must contain the following parameter: 'client_secret or client_assertion'.
                // Explanation: this can happen if your application was not registered as a public client application in Azure AD
                // Mitigation: in the Azure portal, edit the manifest for your application and set the `allowPublicClient` to `true`

                logger.LogCritical(ex, "Error acquiring token of type {Type}.", typeof(MsalServiceException));
            }
            catch (MsalClientException ex)
            {
                // Error Code: unknown_user Message: Could not identify logged in user
                // Explanation: the library was unable to query the current Windows logged-in user or this user is not AD or Azure AD
                // joined (work-place joined users are not supported).

                // Mitigation 1: on UWP, check that the application has the following capabilities: Enterprise Authentication,
                // Private Networks (Client and Server), User Account Information

                // Mitigation 2: Implement your own logic to fetch the username (e.g. john@contoso.com) and use the
                // AcquireTokenByIntegratedWindowsAuth form that takes in the username

                // Error Code: integrated_windows_auth_not_supported_managed_user
                // Explanation: This method relies on an a protocol exposed by Active Directory (AD). If a user was created in Azure
                // Active Directory without AD backing ("managed" user), this method will fail. Users created in AD and backed by
                // Azure AD ("federated" users) can benefit from this non-interactive method of authentication.
                // Mitigation: Use interactive authentication
                logger.LogCritical(ex, "Error");
            }
        }

        if (authenticationResult == null)
        {
            throw new InvalidOperationException("Failed to acquire token.");
        }

        logger.LogTrace("Executed  {Method}.", nameof(GetAuthorizationTokenAsync));
        return authenticationResult.AccessToken;
    }

    private static async Task<MsalCacheHelper> CreateCacheHelperAsync()
    {
        var fileName = $"{Metadata.ApplicationNameShort}.msal.cache";

        var storageProperties = new StorageCreationPropertiesBuilder(fileName, Metadata.ApplicationDataPath)
            .WithUnprotectedFile()
            .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);

        return cacheHelper;
    }
}
