using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Extensions;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class NetworkAccessPolicyOptions : IAccessPolicyOptions
{
    public string? AllowedIPs { get; set; }

    public string[] AllowedIPsList => AllowedIPs!.SplitConfiguration();

    public string? AllowedCidrs { get; set; }

    public string[] AllowedCidrsList => AllowedCidrs!.SplitConfiguration();

}
