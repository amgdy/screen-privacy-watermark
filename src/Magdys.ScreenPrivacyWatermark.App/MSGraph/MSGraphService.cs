using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Magdys.ScreenPrivacyWatermark.App.MSGraph;

public class MSGraphService(MSGraphTokenProvider wamAccessTokenProvider)
{
    public GraphServiceClient Client { get; } = new GraphServiceClient(new BaseBearerTokenAuthenticationProvider(wamAccessTokenProvider));
}
