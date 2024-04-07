using Magdys.ScreenPrivacyWatermark.App.Infrastructure.Extensions;

namespace Magdys.ScreenPrivacyWatermark.App.Infrastructure.AccessPolicy;

internal class MacAddressAccessPolicyOptions : IAccessPolicyOptions
{
    public string AllowedMacAddresses { get; set; }

    public string[] AllowedMacAddressesArray => AllowedMacAddresses.SplitConfiguration();
}
