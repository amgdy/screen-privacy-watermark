using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Magdys.ScreenPrivacyWatermark.App.WatermarkProviders.EntraId;

internal class MicrosoftGraphService(WindowsAccountManagerTokenProvider windowsAccountManagerTokenProvider)
{
    public GraphServiceClient Client { get; set; } = new GraphServiceClient(new BaseBearerTokenAuthenticationProvider(windowsAccountManagerTokenProvider));
}
