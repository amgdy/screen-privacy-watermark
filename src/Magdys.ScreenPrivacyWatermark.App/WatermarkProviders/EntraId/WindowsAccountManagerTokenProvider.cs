using Magdys.ScreenPrivacyWatermark.App.Settings;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Magdys.ScreenPrivacyWatermark.App.WatermarkProviders.EntraId;

internal class WindowsAccountManagerTokenProvider(IOptions<EntraIdSettings> settings) : IAccessTokenProvider
{
    private readonly IPublicClientApplication _pca = PublicClientApplicationBuilder
            .Create(settings.Value.ClientId.ToString())
            .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
            .WithAuthority(settings.Value.Authority)
            .Build();

    private readonly MsalCacheHelper _msalCacheHelper = CreateCacheHelperAsync().Result;

    public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();

    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {

        var authenticationResult = await _pca
            .AcquireTokenSilent(settings.Value.Scopes, PublicClientApplication.OperatingSystemAccount)
            .ExecuteAsync(cancellationToken);

        _msalCacheHelper.RegisterCache(_pca.UserTokenCache);

        return authenticationResult.AccessToken;
    }

    private static async Task<MsalCacheHelper> CreateCacheHelperAsync()
    {
        var fileName = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.msalcache.bin";

        var storageProperties = new StorageCreationPropertiesBuilder(fileName, Metadata.ApplicationDataPath)
            .WithUnprotectedFile()
            .Build();


        MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);

        return cacheHelper;
    }
}
